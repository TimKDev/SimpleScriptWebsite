using SimpleScriptWebSite.Services;

namespace SimpleScriptWebSite.Interfaces;

public interface IDockerDotNetRunner
{
    Task<ContainerSession> RunDotNetDllAsync(string dllPath,
        string[]? args = null,
        int? memoryLimit = null,
        double? cpuLimit = null,
        CancellationToken cancellationToken = default);
}