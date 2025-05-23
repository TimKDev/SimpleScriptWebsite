using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class UserSessionRessourceService
{
    private readonly ConcurrentDictionary<Guid, UserSessionResources?> _allocatedResources = new();
    private readonly ILogger<UserSessionRessourceService> _logger;
    private readonly SandboxerConfig _sandboxerConfig;
    private readonly IFingerPrintService _fingerPrintService;
    private readonly IContainerRepository _containerRepository;

    public UserSessionRessourceService(ILogger<UserSessionRessourceService> logger,
        IOptions<SandboxerConfig> sandboxerConfig, IFingerPrintService fingerPrintService,
        IContainerRepository containerRepository)
    {
        _logger = logger;
        _fingerPrintService = fingerPrintService;
        _containerRepository = containerRepository;
        _sandboxerConfig = sandboxerConfig.Value;
    }

    public async Task CreateAsync(WebSocket webSocket, string startCommand, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to start .NET runtime container for DLL: {DllFileName}",
            _sandboxerConfig.DllFileName);
        var userIdentifier = _fingerPrintService.GetUserIdentifier();
        if (userIdentifier is null)
        {
            _logger.LogWarning("Cannot start container: Missing fingerprint for user.");
            return ContainerCreationResult.Create(ContainerCreationStatus.MissingFingerPrintForUser);
        }

        if (!await IsUserAllowedToStartContainerAsync(userIdentifier))
        {
            _logger.LogWarning(
                "User {UserIdentifier} is not allowed to start a new container (limit exceeded or cleanup in progress).",
                userIdentifier);
            return ContainerCreationResult.Create(ContainerCreationStatus.ContainerLimitExceeded);
        }

        var createdContainer = await _containerRepository.CreateAndStartContainerAsync(
            CreateStartCommand(_sandboxerConfig.DllFileName, args),
            ["/ConsoleApp:/app"],
            memoryLimitInMb: _sandboxerConfig.MaxMemoryInMbPerContainer,
            cpuLimitInPercent: _sandboxerConfig.MaxCpuInPercentPerContainer,
            cancellationToken
        );
        _logger.LogInformation("Container created with ID: {ContainerId} for user {UserIdentifier}",
            createdContainer.ContainerId, userIdentifier);

        if (!_containerOrchestrator.TryAddContainer(userIdentifier, createdContainer))
        {
            _logger.LogWarning(
                "Failed to add container {ContainerId} to orchestrator for user {UserIdentifier}. Cleaning up container.",
                createdContainer.ContainerId, userIdentifier);
            await createdContainer.Cleanup();
            return ContainerCreationResult.Create(ContainerCreationStatus.ContainerLimitExceeded);
        }

        _logger.LogInformation("Container {ContainerId} successfully added to orchestrator for user {UserIdentifier}.",
            createdContainer.ContainerId, userIdentifier);

        return ContainerCreationResult.Create(createdContainer, userIdentifier);
    }

    private async Task<bool> IsUserAllowedToStartContainerAsync(string userIdentifier)
    {
        if (_allocatedResources.Keys.Count > _sandboxerConfig.MaxTotalNumberContainers)
        {
            _logger.LogWarning("Max total number of containers ({MaxContainers}) exceeded. Triggering cleanup.",
                _sandboxerConfig.MaxTotalNumberContainers);

            return false;
        }

        // TryAdd is used to reserve a slot conceptually. The actual container info is added later.
        if (_allocatedResources.Values.Count(r => r?.UserId == userIdentifier) <=
            _sandboxerConfig.MaxNumberContainersPerUser)
        {
            _logger.LogInformation("User {UserIdentifier} allowed to start a container.",
                userIdentifier);

            return true;
        }

        _logger.LogWarning(
            "User {UserIdentifier} already has an entry in allocated resources. Assuming not allowed to start another yet.",
            userIdentifier);

        return false;
    }
}