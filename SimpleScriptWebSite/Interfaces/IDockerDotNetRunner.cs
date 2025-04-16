using SimpleScriptWebSite.Models;
using SimpleScriptWebSite.Services;

namespace SimpleScriptWebSite.Interfaces;

public interface IDockerDotNetRunner
{
    Task<ContainerSession> RunDotNetDllAsync(string dllPath,
        string[]? args = null,
        CancellationToken cancellationToken = default);
}