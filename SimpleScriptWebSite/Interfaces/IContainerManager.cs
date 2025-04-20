using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Interfaces;

public interface IContainerManager
{
    Task<ContainerCreationResult> StartDotNetRuntimeContainerAsync(
        string startCommand,
        CancellationToken cancellationToken);
}