using SimpleScriptWebSite.Interfaces;

namespace SimpleScriptWebSite.Services;

public class ContainerManager : IContainerManager
{
    private readonly IContainerOrchestrator _containerOrchestrator;
    private readonly IContainerRepository _containerRepository;
    private readonly IFingerPrintService _fingerPrintService;

    public ContainerManager(IContainerOrchestrator containerOrchestrator, IContainerRepository containerRepository,
        IFingerPrintService fingerPrintService)
    {
        _containerOrchestrator = containerOrchestrator;
        _containerRepository = containerRepository;
        _fingerPrintService = fingerPrintService;
    }

    public async Task<ContainerCreationResult> StartDotNetRuntimeContainerAsync(string startCommand,
        CancellationToken cancellationToken)
    {
        var userIdentifier = _fingerPrintService.GetUserIdentifier();
        if (userIdentifier is null)
        {
            return ContainerCreationResult.Create(ContainerCreationStatus.MissingFingerPrintForUser);
        }

        if (!_containerOrchestrator.IsUserAllowedToStartContainer(userIdentifier))
        {
            return ContainerCreationResult.Create(ContainerCreationStatus.ContainerLimitExceeded);
        }

        var createdContainer = await _containerRepository.CreateAndStartContainerAsync(
            "mcr.microsoft.com/dotnet/runtime:9.0",
            startCommand, ["/ConsoleApp:/app"], 256, 0.5, cancellationToken);

        if (!_containerOrchestrator.TryAddContainer(userIdentifier, createdContainer))
        {
            return ContainerCreationResult.Create(ContainerCreationStatus.ContainerLimitExceeded);
        }

        return ContainerCreationResult.Create(createdContainer);
    }
}