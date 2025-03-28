namespace SimpleScriptWebSite.Controllers;

public interface IDockerDotNetRunner
{
    Task<ContainerSession> RunDotNetDllAsync(
        string dllPath,
        string[]? args = null,
        Dictionary<string, string>? environmentVariables = null,
        int? memoryLimit = null,
        double? cpuLimit = null,
        CancellationToken cancellationToken = default);
}