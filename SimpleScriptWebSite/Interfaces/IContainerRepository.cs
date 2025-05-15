using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Interfaces;

public interface IContainerRepository
{
    Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken = default);

    Task StopContainerAsync(string containerId, CancellationToken cancellationToken = default);

    Task<ContainerSession> CreateAndStartContainerAsync(string startCommand,
        List<string> binds,
        int? memoryLimitInMb = null,
        double? cpuLimitInPercent = null,
        CancellationToken cancellationToken = default);
}