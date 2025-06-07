using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using SimpleScriptWebSite.Extensions;
using SimpleScriptWebSite.Models;
using SimpleScriptWebSite.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddSimpleScriptWebSiteServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<ContainerWatcher>();
builder.Services.Configure<SandboxerConfig>(builder.Configuration.GetSection("Sandboxer"));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthorization();
app.UseWebSockets(app.Environment.IsDevelopment()
    ? new WebSocketOptions()
    {
        KeepAliveInterval = TimeSpan.FromSeconds(15),
        AllowedOrigins =
        {
            "http://localhost:3000", "http://localhost:10000", "http://localhost:40090",
            "https://tim-kempkens.com:40090"
        }
    }
    : new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(15),
        AllowedOrigins =
        {
            "http://localhost:3000", "http://localhost:10000", "http://localhost:40090",
            "https://tim-kempkens.com/simple-script"
        }
    });
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();