using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class UserSessionRessourceService : IUserSessionRessourceService
{
    private readonly ConcurrentDictionary<Guid, UserSessionResources?> _allocatedResources = new();
    private readonly ILogger<UserSessionRessourceService> _logger;
    private readonly SandboxerConfig _sandboxerConfig;


    public UserSessionRessourceService(ILogger<UserSessionRessourceService> logger,
        IOptions<SandboxerConfig> sandboxerConfig)
    {
        _logger = logger;
        _sandboxerConfig = sandboxerConfig.Value;
    }

    public async Task CleanupResourcesAsync()
    {
        var entriesForCleanup = new List<UserSessionResources>();
        foreach (var resource in _allocatedResources)
        {
            if (resource.Value is null)
            {
                continue;
            }

            if (resource.Value.StartedAt <
                DateTime.UtcNow.AddSeconds(-_sandboxerConfig.AllowedMaxLifeTimeContainerInSeconds) ||
                resource.Value.Status is ResourceStatus.Disposed)
            {
                _logger.LogInformation(
                    "Container {ContainerId} for user {UserIdentifier} has expired (started at {StartTime}). Marked for cleanup.",
                    resource.Value.Container?.ContainerId, resource.Key, resource.Value.StartedAt);
                entriesForCleanup.Add(resource.Value);
            }
        }

        _logger.LogInformation("Found {Count} resources to clean up.", entriesForCleanup.Count);
        foreach (var resource in entriesForCleanup)
        {
            await resource.CleanupAsync();
            _allocatedResources.TryRemove(resource.ResourceId, out _);
        }

        _logger.LogInformation("Container cleanup task finished.");
    }

    public async Task CleanupResourceByIdAsync(Guid resourceId)
    {
        if (_allocatedResources.TryGetValue(resourceId, out var userSessionResources) &&
            userSessionResources is not null)
        {
            await userSessionResources.CleanupAsync();
            _allocatedResources.TryRemove(resourceId, out _);
        }
    }

    public async Task<ContainerCreationResult> TryAddContainerAsync(string userIdentifier,
        ContainerSession container,
        CancellationToken cancellationToken)
    {
        foreach (var allocatedResource in _allocatedResources)
        {
            if (allocatedResource.Value is { Status: ResourceStatus.Pending } &&
                allocatedResource.Value.UserId == userIdentifier)
            {
                allocatedResource.Value.Activate(container);

                _logger.LogInformation(
                    "Container {ContainerId} successfully added to resources for user {UserIdentifier}.",
                    container.ContainerId, userIdentifier);

                return ContainerCreationResult.Create(allocatedResource.Value);
            }
        }

        _logger.LogWarning(
            "Failed to add container {ContainerId} to orchestrator for user {UserIdentifier}. Cleaning up container.",
            container.ContainerId, userIdentifier);

        await container.Cleanup();

        return ContainerCreationResult.Create(AddContainerStatus.ContainerLimitExceeded);
    }

    public bool TryReservingResourceEntryForUser(string userIdentifier, WebSocket webSocket)
    {
        if (_allocatedResources.Keys.Count >= _sandboxerConfig.MaxTotalNumberContainers)
        {
            _logger.LogWarning("Max total number of containers ({MaxContainers}) exceeded. Triggering cleanup.",
                _sandboxerConfig.MaxTotalNumberContainers);

            return false;
        }

        if (_allocatedResources.Values.Count(r => r?.UserId == userIdentifier) <
            _sandboxerConfig.MaxNumberContainersPerUser)
        {
            var resourceId = Guid.NewGuid();
            _allocatedResources.TryAdd(resourceId, new UserSessionResources(resourceId, userIdentifier, webSocket));

            _logger.LogInformation("User {UserIdentifier} allowed to start a container and a resource is reserved.",
                userIdentifier);

            return true;
        }

        _logger.LogWarning(
            "User {UserIdentifier} already has an entry in allocated resources. Assuming not allowed to start another yet.",
            userIdentifier);

        return false;
    }
}