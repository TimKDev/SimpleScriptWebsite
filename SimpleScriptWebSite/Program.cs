using SimpleScriptWebSite.Extensions;
using SimpleScriptWebSite.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSimpleScriptWebSiteServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<ContainerWatcher>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevCorsPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
}

var app = builder.Build();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseCors("DevCorsPolicy");
}

app.UseAuthorization();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30),
});
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();