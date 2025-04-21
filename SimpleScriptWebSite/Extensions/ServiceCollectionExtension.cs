using SimpleScriptWebSite.Controllers;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Services;

namespace SimpleScriptWebSite.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddSimpleScriptWebSiteServices(this IServiceCollection services)
    {
        return services.AddSingleton<IContainerOrchestrator, ContainerOrchestrator>()
            .AddScoped<IInputValidator, InputValidator>()
            .AddScoped<IWebSocketHandler, WebSocketHandler>()
            .AddScoped<IContainerManager, ContainerManager>()
            .AddScoped<IFingerPrintService, FingerPrintService>()
            .AddScoped<IContainerRepository, ContainerRepository>();
    }
}