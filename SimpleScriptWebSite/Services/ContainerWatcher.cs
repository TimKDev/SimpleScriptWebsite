using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;
using Microsoft.Extensions.Logging;

namespace SimpleScriptWebSite.Services;

public class ContainerWatcher : BackgroundService
{
    private readonly IContainerOrchestrator _containerOrchestrator;
    private readonly SandboxerConfig _sandboxerConfig;
    private readonly ILogger<ContainerWatcher> _logger;

    public ContainerWatcher(IContainerOrchestrator containerOrchestrator, IOptions<SandboxerConfig> sandboxerConfig,
        ILogger<ContainerWatcher> logger)
    {
        _containerOrchestrator = containerOrchestrator;
        _sandboxerConfig = sandboxerConfig.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ContainerWatcher starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _containerOrchestrator.CleanupContainersAsync();
                _logger.LogInformation("Container cleanup executed.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Container cleanup canceled.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(_sandboxerConfig.WatcherCheckIntervalInSeconds), stoppingToken);
        }

        _logger.LogInformation("ContainerWatcher stopping.");
    }
}