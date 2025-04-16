using SimpleScriptWebSite.Controllers;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Services;

namespace SimpleScriptWebSite.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddSimpleScriptWebSiteServices(this IServiceCollection services)
    {
        return services.AddSingleton<IContainerOrchestrator, ContainerOrchestrator>()
            .AddScoped<IDockerDotNetRunner, DockerDotNetRunner>()
            .AddScoped<IWebSocketHandler, WebSocketHandler>()
            .AddScoped<IFingerPrintService, FingerPrintService>()
            .AddScoped<IContainerRepository, ContainerRepository>();
    }
}