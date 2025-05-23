using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;
using System.Text;

namespace SimpleScriptWebSite.Services;

public class ContainerManager : IContainerManager
{
    private readonly IContainerOrchestrator _containerOrchestrator;
    private readonly IContainerRepository _containerRepository;
    private readonly IFingerPrintService _fingerPrintService;
    private readonly ILogger<ContainerManager> _logger;

    public const string ExecutionCompletedMessage = "EXECUTION_COMPLETED";

    public ContainerManager(IContainerOrchestrator containerOrchestrator,
        IContainerRepository containerRepository,
        IFingerPrintService fingerPrintService,
        ILogger<ContainerManager> logger)
    {
        _containerOrchestrator = containerOrchestrator;
        _containerRepository = containerRepository;
        _fingerPrintService = fingerPrintService;
        _logger = logger;
    }

    public async Task<ContainerCreationResult> StartDotNetRuntimeContainerAsync(string dllFileName,
        CancellationToken cancellationToken,
        string[]? args = null)
    {
        _logger.LogInformation("Attempting to start .NET runtime container for DLL: {DllFileName}", dllFileName);
        var userIdentifier = _fingerPrintService.GetUserIdentifier();
        if (userIdentifier is null)
        {
            _logger.LogWarning("Cannot start container: Missing fingerprint for user.");
            return ContainerCreationResult.Create(ContainerCreationStatus.MissingFingerPrintForUser);
        }

        if (!await _containerOrchestrator.IsUserAllowedToStartContainerAsync(userIdentifier))
        {
            _logger.LogWarning(
                "User {UserIdentifier} is not allowed to start a new container (limit exceeded or cleanup in progress).",
                userIdentifier);
            return ContainerCreationResult.Create(ContainerCreationStatus.ContainerLimitExceeded);
        }

        var createdContainer = await _containerRepository.CreateAndStartContainerAsync(
            CreateStartCommand(dllFileName, args),
            ["/ConsoleApp:/app"],
            memoryLimitInMb: 20,
            cpuLimitInPercent: 10,
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

    private string CreateStartCommand(string dllFileName, string[]? args)
    {
        //Shebang is used in order for the ExecutionCompleteMessage to be sent after script is complete
        var startCommand = new StringBuilder();
        startCommand.AppendLine("#!/bin/sh");
        startCommand.Append($"dotnet /app/{dllFileName}");

        if (args != null && args.Length > 0)
        {
            startCommand.Append(" " + string.Join(" ", args));
        }

        //Notify output receiver that start command is finished.
        startCommand.AppendLine();
        startCommand.Append($"echo \"{ExecutionCompletedMessage}\"");
        var commandString = startCommand.ToString();
        _logger.LogDebug("Created start command for DLL {DllFileName}: {StartCommand}", dllFileName, commandString);
        return commandString;
    }
}