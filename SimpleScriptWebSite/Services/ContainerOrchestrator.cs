using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerOrchestrator : IContainerOrchestrator
{
    private readonly ConcurrentDictionary<string, ContainerInformation> _allocatedResources = new();
    private readonly SandboxerConfig _sandboxerConfig;

    public ContainerOrchestrator(IOptions<SandboxerConfig> sandboxerConfig)
    {
        _sandboxerConfig = sandboxerConfig.Value;
    }

    public async Task<bool> IsUserAllowedToStartContainerAsync(string userIdentifier)
    {
        if (_allocatedResources.Keys.Count > _sandboxerConfig.MaxTotalNumberContainers)
        {
            await CleanupContainersAsync();
            return false;
        }

        if (_allocatedResources.ContainsKey(userIdentifier))
        {
            return false;
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

    public async Task CleanupContainersAsync()
    {
        var keysForCleanup = new List<string>();
        foreach (var ressource in _allocatedResources)
        {
            if (ressource.Value.StartedAt <
                DateTime.UtcNow.AddSeconds(-_sandboxerConfig.AllowedMaxLifeTimeContainerInSeconds))
            {
                keysForCleanup.Add(ressource.Key);
            }
        }

        foreach (var key in keysForCleanup)
        {
            await RemoveResourcesForUserAsync(key);
        }
    }


    public async Task<bool> RemoveResourcesForUserAsync(string userIdentifier)
    {
        if (_allocatedResources.TryRemove(userIdentifier, out var removedContainer))
        {
            await removedContainer.Session.Cleanup();
            return true;
        }

        return false;
    }
}