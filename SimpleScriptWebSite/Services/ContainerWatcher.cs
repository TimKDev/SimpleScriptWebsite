namespace SimpleScriptWebSite.Services;

public class ContainerWatcher : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //Überprüfe im ContainerOrchestrator welche Docker Container älter als 30 Sekunden sind und entferne sie
        //=> Wichtig beachte, dass dies mit möglichst wenig Exceptions passieren sollte, da es erwartetes Verhalten ist
    }
}