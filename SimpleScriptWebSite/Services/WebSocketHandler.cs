using System.Net.WebSockets;
using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Extensions;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

internal class WebSocketHandler : IWebSocketHandler
{
    private readonly IInputValidator _inputValidator;
    private readonly IContainerManager _containerManager;
    private readonly SandboxerConfig _sandboxerConfig;
    private readonly IContainerOrchestrator _containerOrchestrator;
    private readonly ILogger<WebSocketHandler> _logger;

    public WebSocketHandler(IInputValidator inputValidator, IContainerManager containerManager,
        IOptions<SandboxerConfig> sandboxerConfig,
        IContainerOrchestrator containerOrchestrator, ILogger<WebSocketHandler> logger)
    {
        _inputValidator = inputValidator;
        _containerManager = containerManager;
        _containerOrchestrator = containerOrchestrator;
        _logger = logger;
        _sandboxerConfig = sandboxerConfig.Value;
    }

    public async Task HandleWebSocketConnectionAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        try //This try is the global Error handling of the web socket.
        {
            using var timeoutCts =
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(_sandboxerConfig.AllowedMaxLifeTimeContainerInSeconds));

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            ContainerCreationResult? creationResult = null;

            try
            {
                var startCommand = await webSocket.WaitForMessageAsync(linkedCts.Token);
                if (webSocket.State == WebSocketState.Closed)
                {
                    return;
                }

                if (!_inputValidator.ValidateStartCommand(startCommand))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Start Command is invalid",
                        cancellationToken);
                    return;
                }

                using var executionCompletedCts = new CancellationTokenSource();
                using var linkedCtsExecution =
                    CancellationTokenSource.CreateLinkedTokenSource(executionCompletedCts.Token, linkedCts.Token);

                creationResult =
                    await StartDockerSessionAsync(webSocket, _sandboxerConfig.DllFileName, [startCommand],
                        executionCompletedCts);

                if (creationResult.Status != ContainerCreationStatus.Success)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation,
                        "Container creation is not allowed.",
                        cancellationToken);
                    return;
                }

                await PassInputsOnToContainer(webSocket, creationResult.Session!, linkedCtsExecution.Token);
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection timeout after 30 seconds",
                        CancellationToken.None);
                }
            }
            finally
            {
                if (creationResult?.UserIdentifier != null)
                {
                    await _containerOrchestrator.RemoveResourcesForUserAsync(creationResult.UserIdentifier);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unhandeled Websocket error occured: {e.Message}");
        }
    }

    private async Task PassInputsOnToContainer(WebSocket webSocket, ContainerSession dockerSession,
        CancellationToken cancellationToken)
    {
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var message = await webSocket.WaitForMessageAsync(cancellationToken);
                if (webSocket.State == WebSocketState.Closed)
                {
                    return;
                }

                if (!_inputValidator.ValidateInput(message))
                {
                    await webSocket.SendMessageAsync("Invalid input.", cancellationToken);
                    continue;
                }

                await dockerSession.SendInputAsync(message, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task<ContainerCreationResult> StartDockerSessionAsync(WebSocket webSocket, string consoleFileName,
        string[] args,
        CancellationTokenSource executionCompletedCts)
    {
        var containerCreationResult =
            await _containerManager.StartDotNetRuntimeContainerAsync(consoleFileName, executionCompletedCts.Token,
                args);

        if (containerCreationResult.Status != ContainerCreationStatus.Success)
        {
            return containerCreationResult;
        }

        var containerSession = containerCreationResult.Session!;

        containerSession.OutputReceived += async (_, output) =>
        {
            try
            {
                if (executionCompletedCts.IsCancellationRequested)
                {
                    return;
                }

                if (string.IsNullOrEmpty(output))
                {
                    return;
                }

                if (output == ContainerManager.ExecutionCompletedMessage + "\n")
                {
                    await executionCompletedCts.CancelAsync();
                    return;
                }

                if (_sandboxerConfig.IgnoredOutputs.Contains(output.Trim()))
                {
                    return;
                }

                await webSocket.SendMessageAsync($"output:{output}", executionCompletedCts.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in WebSocket OutputReceived event handler: {Message}", e.Message);
            }
        };

        containerSession.ErrorReceived += async (_, error) =>
        {
            try
            {
                if (!string.IsNullOrEmpty(error) && !executionCompletedCts.IsCancellationRequested)
                {
                    await webSocket.SendMessageAsync($"error:{error}", executionCompletedCts.Token);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in WebSocket ErrorReceived event handler: {Message}", e.Message);
            }
        };

        return containerCreationResult;
    }
}