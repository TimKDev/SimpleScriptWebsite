using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public interface IContainerOrchestrator
{
    bool IsUserAllowedToStartContainer(string userIdentifier);

    bool TryAddContainer(string userIdentifier, ContainerSession containerSession);
    void CleanupContainers();
}