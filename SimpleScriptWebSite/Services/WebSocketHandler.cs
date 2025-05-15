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

    public WebSocketHandler(IInputValidator inputValidator, IContainerManager containerManager,
        IOptions<SandboxerConfig> sandboxerConfig, 
        IContainerOrchestrator containerOrchestrator)
    {
        _inputValidator = inputValidator;
        _containerManager = containerManager;
        _containerOrchestrator = containerOrchestrator;
        _sandboxerConfig = sandboxerConfig.Value;
    }

    public async Task HandleWebSocketConnectionAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        using var timeoutCts =
            new CancellationTokenSource(TimeSpan.FromSeconds(_sandboxerConfig.AllowedMaxLifeTimeContainerInSeconds));

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
                await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Container creation is not allowed.",
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
                    await webSocket.SendMessageAsync("Invalid input.");
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
            if (!string.IsNullOrEmpty(output))
            {
                if (output == ContainerManager.ExecutionCompletedMessage + "\n")
                {
                    await executionCompletedCts.CancelAsync();
                    return;
                }

                await webSocket.SendMessageAsync($"output:{output}");
            }
        };

        containerSession.ErrorReceived += async (_, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                await webSocket.SendMessageAsync($"error:{error}");
            }
        };

        return containerCreationResult;
    }
}