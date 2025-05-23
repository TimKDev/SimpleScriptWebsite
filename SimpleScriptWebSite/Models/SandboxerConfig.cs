namespace SimpleScriptWebSite.Models;

public class SandboxerConfig
{
    public string DllFileName { get; init; } = null!;
    public int MaxTotalNumberContainers { get; init; }
    public int MaxNumberContainersPerUser { get; init; }
    public int MaxMemoryInMbPerContainer { get; init; }
    public int MaxCpuInPercentPerContainer { get; init; }
    public int AllowedMaxLifeTimeContainerInSeconds { get; init; }
    public int WatcherCheckIntervalInSeconds { get; init; }
    public int MaxRequestsPerMinute { get; init; }
    public string[] IgnoredOutputs { get; init; } = [];
}