using System.Net.WebSockets;

namespace SimpleScriptWebSite.Interfaces;

public interface IWebSocketHandler
{
    Task HandleWebSocketConnectionAsync(WebSocket webSocket, CancellationToken cancellationToken);
}