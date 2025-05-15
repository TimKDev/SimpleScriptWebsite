using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerManager : IContainerManager
{
    private readonly IContainerOrchestrator _containerOrchestrator;
    private readonly IContainerRepository _containerRepository;
    private readonly IFingerPrintService _fingerPrintService;

    public const string ExecutionCompletedMessage = "EXECUTION_COMPLETED";

    public ContainerManager(IContainerOrchestrator containerOrchestrator,
        IContainerRepository containerRepository,
        IFingerPrintService fingerPrintService)
    {
        _containerOrchestrator = containerOrchestrator;
        _containerRepository = containerRepository;
        _fingerPrintService = fingerPrintService;
    }

    public async Task<ContainerCreationResult> StartDotNetRuntimeContainerAsync(string dllFileName,
        CancellationToken cancellationToken,
        string[]? args = null)
    {
        var userIdentifier = _fingerPrintService.GetUserIdentifier();
        if (userIdentifier is null)
        {
            return ContainerCreationResult.Create(ContainerCreationStatus.MissingFingerPrintForUser);
        }

        if (!await _containerOrchestrator.IsUserAllowedToStartContainerAsync(userIdentifier))
        {
            return ContainerCreationResult.Create(ContainerCreationStatus.ContainerLimitExceeded);
        }

        var createdContainer = await _containerRepository.CreateAndStartContainerAsync(CreateStartCommand(dllFileName, args), ["/ConsoleApp:/app"], 50, 0.005, cancellationToken);

        if (!_containerOrchestrator.TryAddContainer(userIdentifier, createdContainer))
        {
            await createdContainer.Cleanup();
            return ContainerCreationResult.Create(ContainerCreationStatus.ContainerLimitExceeded);
        }

        return ContainerCreationResult.Create(createdContainer, userIdentifier);
    }

    private string CreateStartCommand(string dllFileName, string[]? args)
    {
        //Shebang is used in order for the ExecutionCompleteMessage to be sent after script is complete
        var startCommand = "#!/bin/sh\n";
        startCommand += $"dotnet /app/{dllFileName}";

        if (args != null && args.Length > 0)
        {
            startCommand += " " + string.Join(" ", args);
        }

        //Notify output receiver that start command is finished.
        startCommand += $"\n echo \"{ExecutionCompletedMessage}\"";

        return startCommand;
    }
}