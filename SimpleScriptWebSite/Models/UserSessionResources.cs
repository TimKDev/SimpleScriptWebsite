using System.Net.WebSockets;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class UserSessionResources
{
    public string UserId { get; }
    public DateTime StartedAt { get; }
    public ContainerSession? Container { get; private set; }
    public Guid ResourceId { get; private set; }
    public WebSocket WebSocket { get; }
    public ResourceStatus Status { get; private set; }
    public int NumberReceivedOutputs { get; private set; }

    public UserSessionResources(Guid resourceId, string userId, WebSocket webSocket)
    {
        StartedAt = DateTime.UtcNow;
        UserId = userId;
        ResourceId = resourceId;
        WebSocket = webSocket;
        Status = ResourceStatus.Pending;
    }

    public void Activate(ContainerSession session)
    {
        Container = session;
        Status = ResourceStatus.Active;
    }

    public void ReceivedOutputOrError()
    {
        NumberReceivedOutputs++;
    }

    public async Task CleanupAsync()
    {
        if (Status == ResourceStatus.Disposed || Status == ResourceStatus.CurrentlyDisposing)
        {
            return;
        }

        Status = ResourceStatus.CurrentlyDisposing;
        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        if (Container != null)
        {
            await Container.Cleanup();
        }

        Status = ResourceStatus.Disposed;
    }
}