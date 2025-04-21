using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Interfaces;

public interface IContainerOrchestrator
{
    Task<bool> IsUserAllowedToStartContainerAsync(string userIdentifier);

    bool TryAddContainer(string userIdentifier, ContainerSession containerSession);

    Task CleanupContainersAsync();

    Task<bool> RemoveResourcesForUserAsync(string userIdentifier);
}