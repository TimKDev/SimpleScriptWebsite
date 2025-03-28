using SimpleScriptWebSite.Controllers;

namespace SimpleScriptWebSite.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddSimpleScriptWebSiteServices(this IServiceCollection services)
    {
        return services.AddScoped<IDockerDotNetRunner, DockerDotNetRunner>();
    }
}