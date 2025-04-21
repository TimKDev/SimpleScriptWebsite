using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Interfaces;

public interface IContainerManager
{
    Task<ContainerCreationResult> StartDotNetRuntimeContainerAsync(string dllFileName,
        CancellationToken cancellationToken,
        string[]? args = null);
}