using System.Collections.Concurrent;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

internal class DockerDotNetRunner : IDockerDotNetRunner
{
    private readonly ILogger<DockerDotNetRunner> _logger;
    private readonly IContainerRepository _containerRepository;
    private readonly IFingerPrintService _fingerPrintService;
    private readonly IContainerOrchestrator _containerOrchestrator;

    public const string ExecutionCompletedMessage = "EXECUTION_COMPLETED";

    public DockerDotNetRunner(ILogger<DockerDotNetRunner> logger, IContainerRepository containerRepository,
        IContainerOrchestrator containerOrchestrator, IFingerPrintService fingerPrintService)
    {
        _logger = logger;
        _containerRepository = containerRepository;
        _containerOrchestrator = containerOrchestrator;
        _fingerPrintService = fingerPrintService;
    }

    public Task<ContainerSession> RunDotNetDllAsync(
        string dllFileName,
        string[]? args = null,
        CancellationToken cancellationToken = default)
    {
        //Shebang is used in order for the ExecutionCompleteMessage to be send after script is complete
        var startCommand = "#!/bin/sh\n";
        startCommand += $"dotnet /app/{dllFileName}";

        if (args != null && args.Length > 0)
        {
            startCommand += " " + string.Join(" ", args);
        }

        //Notify output receiver that start command is finished.
        startCommand += $"\n echo \"{ExecutionCompletedMessage}\"";


        var userFingerPrint = _fingerPrintService.GetUserIdentifier() ?? Guid.NewGuid().ToString();

        //Überprüfe, ob der aktuelle User noch mehr Container anlegen darf, falls nicht verlasse hier diese Funktion und
        //beende den WebSocket sauber.

        return _containerRepository.CreateAndStartContainerAsync("mcr.microsoft.com/dotnet/runtime:9.0",
            startCommand, ["/ConsoleApp:/app"], 256, 0.5, cancellationToken);
    }
}

public class ContainerOrchestrator : IContainerOrchestrator
{
    private readonly ConcurrentDictionary<string, ContainerInformation> _allocatedResources = new();

    public async Task<bool> IsUserAllowedToStartContainer(string userIdentifier)
    {
        //Check total Number is not exceeded 
        //Check if total Number of User is not exceeded
        return true;
    }
}

public interface IContainerOrchestrator
{
}

public class ContainerInformation
{
    public ContainerSession Session { get; set; }
    public DateTime StartedAt { get; set; }
}