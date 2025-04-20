using SimpleScriptWebSite.Interfaces;

namespace SimpleScriptWebSite.Services;

internal class DockerDotNetRunner : IDockerDotNetRunner
{
    private readonly ILogger<DockerDotNetRunner> _logger;
    private readonly IContainerManager _containerManager;

    public const string ExecutionCompletedMessage = "EXECUTION_COMPLETED";

    public DockerDotNetRunner(ILogger<DockerDotNetRunner> logger,
        IContainerManager containerManager)
    {
        _logger = logger;
        _containerManager = containerManager;
    }

    public Task<ContainerCreationResult> RunDotNetDllAsync(string dllFileName,
        string[]? args = null,
        CancellationToken cancellationToken = default)
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

        return _containerManager.StartDotNetRuntimeContainerAsync(startCommand, cancellationToken);
    }
}