using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Services;

namespace SimpleScriptWebSite.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsoleAppController : ControllerBase
{
    private readonly IWebSocketHandler _webSocketHandler;
    private readonly IFingerPrintService _fingerPrintService;

    public ConsoleAppController(IWebSocketHandler webSocketHandler, IFingerPrintService fingerPrintService)
    {
        _webSocketHandler = webSocketHandler;
        _fingerPrintService = fingerPrintService;
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