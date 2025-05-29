using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Extensions;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

internal class WebSocketHandler : IWebSocketHandler
{
    private readonly IInputValidator _inputValidator;
    private readonly SandboxerConfig _sandboxerConfig;
    private readonly ILogger<WebSocketHandler> _logger;
    private readonly IUserSessionRessourceService _userSessionRessourceService;
    private readonly IFingerPrintService _fingerPrintService;
    private readonly IContainerRepository _containerRepository;

    public const string ExecutionCompletedMessage = "EXECUTION_COMPLETED";

    public WebSocketHandler(IInputValidator inputValidator,
        IOptions<SandboxerConfig> sandboxerConfig,
        ILogger<WebSocketHandler> logger,
        IUserSessionRessourceService userSessionRessourceService, IFingerPrintService fingerPrintService,
        IContainerRepository containerRepository)
    {
        _inputValidator = inputValidator;
        _logger = logger;
        _userSessionRessourceService = userSessionRessourceService;
        _fingerPrintService = fingerPrintService;
        _containerRepository = containerRepository;
        _sandboxerConfig = sandboxerConfig.Value;
    }

    public async Task HandleWebSocketConnectionAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        ContainerCreationResult? creationResult = null;
        try
        {
            var startCommand = await webSocket.WaitForMessageAsync(cancellationToken);
            if (webSocket.State == WebSocketState.Closed)
            {
                return;
            }

            if (!_inputValidator.ValidateStartCommand(startCommand))
            {
                await CloseWebsocketAsPolicyViolationAsync(webSocket, "Start Command is invalid", cancellationToken);
                return;
            }

            var userIdentifier = _fingerPrintService.GetUserIdentifier();
            if (userIdentifier is null)
            {
                var message = "Cannot start container: Missing fingerprint for user.";
                await CloseWebsocketAsPolicyViolationAsync(webSocket, message, cancellationToken);
                return;
            }

            _logger.LogInformation("Attempting to start .NET runtime container for DLL: {DllFileName}",
                _sandboxerConfig.DllFileName);

            if (!_userSessionRessourceService.TryReservingResourceEntryForUser(userIdentifier, webSocket))
            {
                await CloseWebsocketAsPolicyViolationAsync(webSocket,
                    $"User {userIdentifier} is not allowed to start a new container.", cancellationToken);
                return;
            }

            var createdContainer = await _containerRepository.CreateAndStartContainerAsync(
                CreateStartCommand(_sandboxerConfig.DllFileName, startCommand),
                ["/ConsoleApp:/app"],
                memoryLimitInMb: _sandboxerConfig.MaxMemoryInMbPerContainer,
                cpuLimitInPercent: _sandboxerConfig.MaxCpuInPercentPerContainer,
                cancellationToken
            );

            _logger.LogInformation("Container created with ID: {ContainerId} for user {UserIdentifier}",
                createdContainer.ContainerId, userIdentifier);

            creationResult =
                await _userSessionRessourceService.TryAddContainerAsync(userIdentifier, createdContainer,
                    cancellationToken);

            if (creationResult.Status != AddContainerStatus.Success)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation,
                    "Container creation is not allowed.",
                    cancellationToken);
                return;
            }

            var resource = creationResult.Resource!;
            resource.Container!.OutputReceived += OnOutputReceivedAsync(webSocket, cancellationToken, resource);
            resource.Container.ErrorReceived += OnErrorReceivedAsync(webSocket, resource, cancellationToken);

            await PassInputsOnToContainer(resource, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unhandeled Websocket error occured: {e.Message}");
        }
        finally
        {
            if (creationResult?.Resource is not null)
            {
                await _userSessionRessourceService.CleanupResourceByIdAsync(creationResult.Resource.ResourceId);
            }
        }
    }

    private async Task CloseWebsocketAsPolicyViolationAsync(WebSocket webSocket, string message,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(message);
        await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, message, cancellationToken);
    }

    private string CreateStartCommand(string dllFileName, string? args)
    {
        //Shebang is used in order for the ExecutionCompleteMessage to be sent after script is complete
        var startCommand = new StringBuilder();
        startCommand.AppendLine("#!/bin/sh");
        startCommand.Append($"dotnet /app/{dllFileName}");

        if (!string.IsNullOrWhiteSpace(args))
        {
            startCommand.Append(" " + args);
        }

        //Notify output receiver that start command is finished.
        startCommand.AppendLine();
        startCommand.Append($"echo \"{ExecutionCompletedMessage}\"");
        var commandString = startCommand.ToString();
        _logger.LogDebug("Created start command for DLL {DllFileName}: {StartCommand}", dllFileName, commandString);
        return commandString;
    }

    private EventHandler<string>? OnErrorReceivedAsync(WebSocket webSocket, UserSessionResources resource,
        CancellationToken cancellationToken)
    {
        return async (_, error) =>
        {
            try
            {
                resource.ReceivedOutputOrError();

                if (resource.NumberReceivedOutputs > _sandboxerConfig.MaxOutputsPerContainer)
                {
                    await resource.CleanupAsync();
                    return;
                }

                if (!string.IsNullOrEmpty(error))
                {
                    await webSocket.SendMessageAsync($"error:{error}", cancellationToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in WebSocket ErrorReceived event handler: {Message}", e.Message);
            }
        };
    }

    private EventHandler<string>? OnOutputReceivedAsync(WebSocket webSocket, CancellationToken cancellationToken,
        UserSessionResources resource)
    {
        return async (_, output) =>
        {
            try
            {
                if (string.IsNullOrEmpty(output))
                {
                    return;
                }

                if (output == ExecutionCompletedMessage + "\n")
                {
                    await resource.CleanupAsync();
                    return;
                }

                resource.ReceivedOutputOrError();

                if (resource.NumberReceivedOutputs > _sandboxerConfig.MaxOutputsPerContainer)
                {
                    await resource.CleanupAsync();
                    return;
                }

                if (_sandboxerConfig.IgnoredOutputs.Contains(output.Trim()))
                {
                    return;
                }

                await webSocket.SendMessageAsync($"output:{output}", cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in WebSocket OutputReceived event handler: {Message}", e.Message);
            }
        };
    }

    private async Task PassInputsOnToContainer(UserSessionResources resource,
        CancellationToken cancellationToken)
    {
        var webSocket = resource.WebSocket;
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Client requested closing the websocket connection.");
                    await resource.CleanupAsync();
                    return;
                }

                if (receiveResult.MessageType != WebSocketMessageType.Text)
                {
                    _logger.LogInformation("Client send invalid message type via websocket. Closing connection.");
                    await resource.CleanupAsync();
                    return;
                }

                if (receiveResult.Count > 1024 * 4)
                {
                    _logger.LogInformation("Message size sent by client via websocket is too big. Closing connection.");
                    await resource.CleanupAsync();
                    return;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                if (!_inputValidator.ValidateInput(message))
                {
                    _logger.LogInformation("User provided invalid input to websocket.");
                    await webSocket.SendMessageAsync("Invalid input.", cancellationToken);
                    continue;
                }

                await resource.Container!.SendInputAsync(message, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
    }
}