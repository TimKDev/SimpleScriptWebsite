using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerOrchestrator : IContainerOrchestrator
{
    private readonly ConcurrentDictionary<string, ContainerInformation?> _allocatedResources = new();
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

        return _allocatedResources.TryAdd(userIdentifier, null);
    }

    public bool TryAddContainer(string userIdentifier, ContainerSession containerSession)
    {
        var newContainerInformation = new ContainerInformation()
        {
            Session = containerSession,
            StartedAt = DateTime.UtcNow,
        };
        var oldContainerInformation = _allocatedResources.GetOrAdd(userIdentifier, newContainerInformation);
        return oldContainerInformation is null && _allocatedResources.TryUpdate(userIdentifier, newContainerInformation, null);
    }

    public async Task CleanupContainersAsync()
    {
        var keysForCleanup = new List<string>();
        foreach (var ressource in _allocatedResources)
        {
            if (ressource.Value is null)
            {
                continue;
            }
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
        if (_allocatedResources.TryGetValue(userIdentifier, out ContainerInformation? containerInformation) && containerInformation?.Session is not null)
        {
            try
            {
                //TODO Ein Problem ist, dass der Cleanup länger dauert als 5 Sekunden, d.h. der User kann zwei Requests erst nach 5 Sekunden
                //zueinander abschicken => Wird in der UX ein Problem
                await containerInformation.Session.Cleanup();
            }
            catch
            {
                return false;
            }
            return _allocatedResources.TryRemove(userIdentifier, out _);
             
        }
        return false;
    }
}