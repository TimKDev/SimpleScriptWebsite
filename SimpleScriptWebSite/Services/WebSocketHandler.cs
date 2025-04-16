using System.Net.WebSockets;
using SimpleScriptWebSite.Extensions;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

internal class WebSocketHandler : IWebSocketHandler
{
    private readonly IDockerDotNetRunner _dockerDotNetRunner;

    public WebSocketHandler(IDockerDotNetRunner dockerDotNetRunner)
    {
        _dockerDotNetRunner = dockerDotNetRunner;
    }

    public async Task HandleWebSocketConnectionAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var startCommand = await webSocket.WaitForMessageAsync(cancellationToken);

        using var executionCompletedCts = new CancellationTokenSource();
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, executionCompletedCts.Token);

        using var dockerSession =
            await StartDockerSessionAsync(webSocket, "HelloWorld2.dll", [startCommand], executionCompletedCts);

        await PassInputsOnToContainer(webSocket, dockerSession, linkedCts.Token);
    }

    private static async Task PassInputsOnToContainer(WebSocket webSocket, ContainerSession dockerSession,
        CancellationToken cancellationToken)
    {
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var message = await webSocket.WaitForMessageAsync(cancellationToken);
                await dockerSession.SendInputAsync(message, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task<ContainerSession> StartDockerSessionAsync(WebSocket webSocket, string consoleFileName,
        string[] args,
        CancellationTokenSource executionCompletedCts)
    {
        var dockerSession =
            await _dockerDotNetRunner.RunDotNetDllAsync(consoleFileName, args);

        dockerSession.OutputReceived += async (_, output) =>
        {
            if (!string.IsNullOrEmpty(output))
            {
                if (output == DockerDotNetRunner.ExecutionCompletedMessage + "\n")
                {
                    await executionCompletedCts.CancelAsync();
                    return;
                }

                await webSocket.SendMessageAsync($"output:{output}");
            }
        };

        dockerSession.ErrorReceived += async (_, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                await webSocket.SendMessageAsync($"error:{error}");
            }
        };

        return dockerSession;
    }
}