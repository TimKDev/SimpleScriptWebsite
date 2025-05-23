using System.Net.WebSockets;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class UserSessionResources
{
    public string UserId { get; }
    public DateTime StartedAt { get; }
    public ContainerSession Session { get; }
    public WebSocket WebSocket { get; }

    public UserSessionResources(string userId, ContainerSession session, WebSocket webSocket)
    {
        StartedAt = DateTime.UtcNow;
        UserId = userId;
        Session = session;
        WebSocket = webSocket;
    }

    public async Task Cleanup()
    {
        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        await Session.Cleanup();
    }
}