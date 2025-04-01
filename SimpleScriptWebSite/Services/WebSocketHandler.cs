using System.Net.WebSockets;
using System.Text;
using SimpleScriptWebSite.Interfaces;

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
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), cancellationToken);

        if (receiveResult.MessageType != WebSocketMessageType.Text)
        {
            throw new InvalidOperationException("Unexpected message type");
        }

        var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        using var dockerSession = await StartDockerSessionAsync(webSocket, receivedMessage);

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        var processExited = false; //TODO Finde heraus das der Prozess im Docker korrekt beendet wurde

        while (!processExited && !timeoutTask.IsCompleted && webSocket.State == WebSocketState.Open)
        {
            try
            {
                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);

                // Check if client initiated close
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    // Acknowledge the close request
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing as requested by client",
                        cancellationToken);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                if (message.StartsWith("input"))
                {
                    //websocket input form: "input:{value}"
                    var inputValue = message.Split(":")[1];
                    await dockerSession.SendInputAsync(inputValue, cancellationToken);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private async Task SendMessageAsync(WebSocket webSocket, string message)
    {
        if (webSocket.State == WebSocketState.Closed)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(message);
        var arraySegment = new ArraySegment<byte>(bytes);
        await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task<ContainerSession> StartDockerSessionAsync(WebSocket webSocket, string command)
    {
        var dockerSession =
            await _dockerDotNetRunner.RunDotNetDllAsync("ConsoleApp/HelloWorld.dll", null, 256, 0.5);

        dockerSession.OutputReceived += async (_, e) =>
        {
            if (!string.IsNullOrEmpty(e))
            {
                await SendMessageAsync(webSocket, $"output:{e}");
            }
        };

        dockerSession.ErrorReceived += async (_, e) =>
        {
            if (!string.IsNullOrEmpty(e))
            {
                await SendMessageAsync(webSocket, $"error:{e}");
            }
        };

        return dockerSession;
    }
}