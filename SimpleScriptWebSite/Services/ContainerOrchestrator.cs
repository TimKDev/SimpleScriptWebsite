using System.Collections.Concurrent;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerOrchestrator : IContainerOrchestrator
{
    private readonly ConcurrentDictionary<string, ContainerInformation> _allocatedResources = new();
    private const int MaxTotalNumber = 10;
    private const int AllowedMaxLifeTimeInSeconds = 30;

    public bool IsUserAllowedToStartContainer(string userIdentifier)
    {
        if (_allocatedResources.Keys.Count > MaxTotalNumber)
        {
            CleanupContainers();
            return false;
        }

        if (_allocatedResources.ContainsKey(userIdentifier))
        {
            return RemoveContainer(userIdentifier);
        }

        return true;
    }

    public bool TryAddContainer(string userIdentifier, ContainerSession containerSession)
    {
        return _allocatedResources.TryAdd(userIdentifier, new ContainerInformation()
        {
            Session = containerSession,
            StartedAt = DateTime.UtcNow,
        });
    }

    public void CleanupContainers()
    {
        var keysForCleanup = new List<string>();
        foreach (var ressource in _allocatedResources)
        {
            if (ressource.Value.StartedAt < DateTime.UtcNow.AddSeconds(-AllowedMaxLifeTimeInSeconds))
            {
                keysForCleanup.Add(ressource.Key);
            }
        }

        foreach (var key in keysForCleanup)
        {
            RemoveContainer(key);
        }
    }


    private bool RemoveContainer(string userIdentifier)
    {
        if (_allocatedResources.TryRemove(userIdentifier, out var removedContainer))
        {
            removedContainer.Session.Dispose();
            return true;
        }

        return false;
    }
}