namespace SimpleScriptWebSite.Services;

public interface IContainerManager
{
    Task<ContainerCreationResult> StartDotNetRuntimeContainerAsync(string startCommand,
        CancellationToken cancellationToken);
}