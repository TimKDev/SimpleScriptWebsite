using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Interfaces;

public interface IContainerRepository
{
    Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken = default);

    Task StopContainerAsync(string containerId, CancellationToken cancellationToken = default);

    Task<ContainerSession> CreateAndStartContainerAsync(
        string imageName,
        string startCommand,
        List<string> binds,
        int? memoryLimit = null,
        double? cpuLimit = null,
        CancellationToken cancellationToken = default);
}