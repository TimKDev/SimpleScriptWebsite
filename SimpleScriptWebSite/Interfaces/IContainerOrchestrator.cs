using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Interfaces;

public interface IWebSocketResourceManager
{
    bool IsUserAllowedToStartContainer(string userIdentifier);

    bool TryAddContainer(string userIdentifier, ContainerSession containerSession);

    void CleanupContainers();
}