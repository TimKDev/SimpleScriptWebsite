using System.Net.WebSockets;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public interface IUserSessionRessourceService
{
    Task CleanupResourcesAsync();

    Task CleanupResourceByIdAsync(Guid resourceId);
    bool TryReservingResourceEntryForUser(string userIdentifier, WebSocket webSocket);

    Task<ContainerCreationResult> TryAddContainerAsync(string userIdentifier,
        ContainerSession container,
        CancellationToken cancellationToken);
}