using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;

namespace SimpleScriptWebSite.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsoleAppController : ControllerBase
{
    /*
     * TODOS:
     * - Starte den Process in einem Docker Container mit nur geringen Resourcen => Docker in Docker Szenario verstehen
     * - Statt für jedes Request einen Docker zu erstellen, verwende lieber einen Docker Pool der dann von den unterschiedlichen Requests
     * verwendet werden kann
     * - Abstrahiere diese Funktionalität zu einer eigenen Library, die generell verwendet werden kann, um Consolenanwendungen im Web darzustellen
     * - Definiere entsprechende Settings für diese Library
     */

    [HttpGet("/ws")]
    public async Task Get(CancellationToken cancellationToken)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocketConnectionAsync(webSocket, cancellationToken);

            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                null,
                CancellationToken.None);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    private async Task HandleWebSocketConnectionAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), cancellationToken);

        if (receiveResult.MessageType != WebSocketMessageType.Text)
        {
            throw new InvalidOperationException("Unexpected message type");
        }

        var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        using var process = CreateDockerizedProcess(webSocket, receivedMessage);
        Start(process);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

        while (!process.HasExited && !timeoutTask.IsCompleted)
        {
            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), cancellationToken);

            var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            await HandleIncomingMessageAsync(process, message, cancellationToken);
        }
    }

    private static void Start(Process process)
    {
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private Process CreateProcess(WebSocket webSocket, string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ConsoleApp/HelloWorld.dll {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                await SendMessageAsync(webSocket, $"output:{e.Data}");
            }
        };

        process.ErrorDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                await SendMessageAsync(webSocket, $"error:{e.Data}");
            }
        };

        return process;
    }

    private async Task SendMessageAsync(WebSocket webSocket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var arraySegment = new ArraySegment<byte>(bytes);
        await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task HandleIncomingMessageAsync(Process process, string message, CancellationToken cancellationToken)
    {
        if (message.StartsWith("input"))
        {
            //websocket input form: "input:{value}"
            var inputValue = message.Split(":")[1];
            await process.StandardInput.WriteLineAsync(inputValue);
        }
    }

    private Process CreateDockerizedProcess(WebSocket webSocket, string command)
    {
        // Create a unique container name for this execution
        string containerId = Guid.NewGuid().ToString("N");
        var testPath = Path.GetFullPath("ConsoleApp");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                // Run the container with:
                // - Resource limits (memory, CPU)
                // - Removed network access
                // - Clean removal when done
                // - Volume mount for the app
                // - Command to run
                Arguments = $"run --rm -i" +
                            $"--name script-container-{containerId} " +
                            $"--memory=128m --cpus=0.2 " +
                            $"--network none " +
                            $"-v {Path.GetFullPath("ConsoleApp")}:/app " +
                            $"--workdir /app " +
                            $"mcr.microsoft.com/dotnet/runtime:9.0 " +
                            $"dotnet HelloWorld.dll {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                await SendMessageAsync(webSocket, $"output:{e.Data}");
            }
        };

        process.ErrorDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                await SendMessageAsync(webSocket, $"error:{e.Data}");
            }
        };

        return process;
    }
}