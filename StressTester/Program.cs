using System.Net.WebSockets;
using System.Text;

namespace StressTester;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("STRESS TESTER");
        Console.WriteLine("Enter number of clients for stress test.");
        var numberOfClients = int.Parse(Console.ReadLine()!);
        var tasks = new List<Task>();
        for (var i = 0; i < numberOfClients; i++)
        {
            tasks.Add(ConnectToWebSocket());
        }
        Task.WaitAll(tasks.ToArray());
    }

    private static async Task ConnectToWebSocket()
    {
        using var ws = new ClientWebSocket();

        try
        {
            Console.WriteLine("Connecting to WebSocket server...");
            ws.Options.SetRequestHeader("Origin", "http://localhost:10000");
            ws.Options.SetRequestHeader("User-Agent", "ConsoleApp");
            ws.Options.SetRequestHeader("Upgrade", "websocket");
            ws.Options.SetRequestHeader("Connection", "Upgrade");
            ws.Options.SetRequestHeader("Sec-WebSocket-Version", "13");
            await ws.ConnectAsync(new Uri("ws://localhost:10000/ws"), CancellationToken.None);
            Console.WriteLine("Connected!");
            
            var bytes = Encoding.UTF8.GetBytes("Hello Client!");
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    CancellationToken.None);
                
            // Start a task to receive messages
            var receiveTask = ReceiveMessagesAsync(ws);

            // Start a task to send messages
            var sendTask = SendMessagesAsync(ws);

            // Wait for both tasks to complete
            await Task.WhenAll(receiveTask, sendTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }

    static async Task ReceiveMessagesAsync(ClientWebSocket ws)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server requested close",
                        CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving message: {ex.Message}");
        }
    }

    static async Task SendMessagesAsync(ClientWebSocket ws)
    {
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                Console.Write("Enter message to send (or 'exit' to quit): ");
                var message = Console.ReadLine();

                if (string.IsNullOrEmpty(message))
                    continue;

                if (message.ToLower() == "exit")
                    break;

                var bytes = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }
}