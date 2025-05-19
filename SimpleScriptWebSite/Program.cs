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

var app = builder.Build();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthorization();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15),
    AllowedOrigins = { "http://localhost:3000", "http://localhost:10000", "https://tim-kempkens.com" }
});
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();