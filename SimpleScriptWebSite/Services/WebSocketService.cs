using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace SimpleScriptWebSite.Services;

public interface IWebSocketService
{
}

public class WebSocketServices : IWebSocketService
{

    public string StartSocket(WebSocket webSocket)
    {
        var socketId = GenerateRandomId(30);

        return socketId;
    }

    private string GenerateRandomId(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}