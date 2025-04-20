using SimpleScriptWebSite.Interfaces;

namespace SimpleScriptWebSite.Services;

public class ContainerWatcher : BackgroundService
{
    private readonly IWebSocketResourceManager _webSocketResourceManager;

    public ContainerWatcher(IWebSocketResourceManager webSocketResourceManager)
    {
        _webSocketResourceManager = webSocketResourceManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _webSocketResourceManager.CleanupContainers();
            await Task.Delay(5000, stoppingToken);
        }
    }
}