using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerWatcher : BackgroundService
{
    private readonly SandboxerConfig _sandboxerConfig;
    private readonly IUserSessionRessourceService _userSessionRessourceService;
    private readonly ILogger<ContainerWatcher> _logger;

    public ContainerWatcher(IOptions<SandboxerConfig> sandboxerConfig,
        ILogger<ContainerWatcher> logger, IUserSessionRessourceService userSessionRessourceService)
    {
        _sandboxerConfig = sandboxerConfig.Value;
        _logger = logger;
        _userSessionRessourceService = userSessionRessourceService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ContainerWatcher starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _userSessionRessourceService.CleanupResourcesAsync();
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