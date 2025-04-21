using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerWatcher : BackgroundService
{
    private readonly IContainerOrchestrator _containerOrchestrator;
    private readonly SandboxerConfig _sandboxerConfig;

    public ContainerWatcher(IContainerOrchestrator containerOrchestrator, IOptions<SandboxerConfig> sandboxerConfig)
    {
        _containerOrchestrator = containerOrchestrator;
        _sandboxerConfig = sandboxerConfig.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _containerOrchestrator.CleanupContainersAsync();
            await Task.Delay(TimeSpan.FromSeconds(_sandboxerConfig.WatcherCheckIntervalInSeconds), stoppingToken);
        }
    }
}