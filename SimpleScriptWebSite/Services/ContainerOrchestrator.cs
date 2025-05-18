using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerOrchestrator : IContainerOrchestrator
{
    private readonly ConcurrentDictionary<string, ContainerInformation?> _allocatedResources = new();
    private readonly SandboxerConfig _sandboxerConfig;
    private readonly ILogger<ContainerOrchestrator> _logger;

    public ContainerOrchestrator(IOptions<SandboxerConfig> sandboxerConfig, ILogger<ContainerOrchestrator> logger)
    {
        _sandboxerConfig = sandboxerConfig.Value;
        _logger = logger;
    }

    public async Task<bool> IsUserAllowedToStartContainerAsync(string userIdentifier)
    {
        if (_allocatedResources.Keys.Count > _sandboxerConfig.MaxTotalNumberContainers)
        {
            _logger.LogWarning("Max total number of containers ({MaxContainers}) exceeded. Triggering cleanup.",
                _sandboxerConfig.MaxTotalNumberContainers);
            await CleanupContainersAsync();
            return false;
        }

        // TryAdd is used to reserve a slot conceptually. The actual container info is added later.
        if (_allocatedResources.TryAdd(userIdentifier, null))
        {
            _logger.LogInformation("User {UserIdentifier} tentatively allowed to start a container (slot reserved).",
                userIdentifier);
            return true;
        }
        else
        {
            _logger.LogWarning(
                "User {UserIdentifier} already has an entry in allocated resources. Assuming not allowed to start another yet.",
                userIdentifier);
            return false;
        }
    }

    public bool TryAddContainer(string userIdentifier, ContainerSession containerSession)
    {
        var newContainerInformation = new ContainerInformation()
        {
            Session = containerSession,
            StartedAt = DateTime.UtcNow,
        };
        var oldContainerInformation = _allocatedResources.GetOrAdd(userIdentifier, newContainerInformation);
        return oldContainerInformation is null &&
               _allocatedResources.TryUpdate(userIdentifier, newContainerInformation, null);
    }

    public async Task CleanupContainersAsync()
    {
        var keysForCleanup = new List<string>();
        foreach (var resource in _allocatedResources)
        {
            if (resource.Value is null)
            {
                continue;
            }

            if (resource.Value.StartedAt <
                DateTime.UtcNow.AddSeconds(-_sandboxerConfig.AllowedMaxLifeTimeContainerInSeconds))
            {
                _logger.LogInformation(
                    "Container {ContainerId} for user {UserIdentifier} has expired (started at {StartTime}). Marked for cleanup.",
                    resource.Value.Session.ContainerId, resource.Key, resource.Value.StartedAt);
                keysForCleanup.Add(resource.Key);
            }
        }

        _logger.LogInformation("Found {Count} containers to clean up.", keysForCleanup.Count);
        foreach (var key in keysForCleanup)
        {
            await RemoveResourcesForUserAsync(key);
        }

        _logger.LogInformation("Container cleanup task finished.");
    }


    public async Task<bool> RemoveResourcesForUserAsync(string userIdentifier)
    {
        _logger.LogInformation("Attempting to remove resources for user {UserIdentifier}.", userIdentifier);
        if (_allocatedResources.TryGetValue(userIdentifier, out ContainerInformation? containerInformation) &&
            containerInformation?.Session is not null)
        {
            try
            {
                //TODO Ein Problem ist, dass der Cleanup länger dauert als 5 Sekunden, d.h. der User kann zwei Requests erst nach 5 Sekunden
                //zueinander abschicken => Wird in der UX ein Problem
                await containerInformation.Session.Cleanup();
                _logger.LogInformation(
                    "Successfully cleaned up session for container {ContainerId} of user {UserIdentifier}.",
                    containerInformation.Session.ContainerId, userIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during container session cleanup for container {ContainerId} of user {UserIdentifier}.",
                    containerInformation.Session.ContainerId, userIdentifier);
                // Do not return false yet, still try to remove from dictionary
            }

            if (_allocatedResources.TryRemove(userIdentifier, out _))
            {
                _logger.LogInformation(
                    "Successfully removed container {ContainerId} reference for user {UserIdentifier} from orchestrator.",
                    containerInformation.Session.ContainerId, userIdentifier);
                return true;
            }

            _logger.LogWarning(
                "Failed to remove container {ContainerId} reference for user {UserIdentifier} from orchestrator, though cleanup might have been attempted.",
                containerInformation.Session.ContainerId, userIdentifier);
            return false; // Failed to remove from dictionary
        }

        return false;
    }
}