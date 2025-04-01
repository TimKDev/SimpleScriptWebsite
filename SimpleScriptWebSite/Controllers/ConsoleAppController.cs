using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
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
            try
            {
                await _webSocketHandler.HandleWebSocketConnectionAsync(webSocket, cancellationToken);
            }
            finally
            {
                if (webSocket.State != WebSocketState.Closed)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        null,
                        CancellationToken.None);
                }
            }
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }
}