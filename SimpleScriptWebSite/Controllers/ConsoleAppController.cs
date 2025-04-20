using Microsoft.AspNetCore.Mvc;
using SimpleScriptWebSite.Interfaces;

namespace SimpleScriptWebSite.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsoleAppController : ControllerBase
{
    private readonly IWebSocketHandler _webSocketHandler;

    public ConsoleAppController(IWebSocketHandler webSocketHandler)
    {
        _webSocketHandler = webSocketHandler;
    }

    [HttpGet("/ws")]
    public async Task Get(CancellationToken cancellationToken)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.HandleWebSocketConnectionAsync(webSocket, cancellationToken);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }
}