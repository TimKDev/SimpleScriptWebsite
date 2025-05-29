using SimpleScriptWebSite.Controllers;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Services;

namespace SimpleScriptWebSite.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddSimpleScriptWebSiteServices(this IServiceCollection services)
    {
        return
            services.AddScoped<IInputValidator, InputValidator>()
                .AddSingleton<IUserSessionRessourceService, UserSessionRessourceService>()
                .AddScoped<IWebSocketHandler, WebSocketHandler>()
                .AddScoped<IFingerPrintService, FingerPrintService>()
                .AddScoped<IContainerRepository, ContainerRepository>();
    }
}