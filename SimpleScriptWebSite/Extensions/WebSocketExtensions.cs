using System.Net.WebSockets;
using System.Text;

namespace SimpleScriptWebSite.Extensions;

internal static class WebSocketExtensions
{
    public static async Task<string> WaitForMessageAsync(this WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), cancellationToken);


        if (receiveResult.MessageType == WebSocketMessageType.Close)
        {
            const string closingMessage = "Closing as requested by client";
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                closingMessage,
                cancellationToken);

            return closingMessage;
        }

        if (receiveResult.MessageType != WebSocketMessageType.Text)
        {
            throw new InvalidOperationException("Unexpected message type");
        }

        if (receiveResult.Count > 1024 * 4)
        {
            const string closingMessage = "Message Size is too large";
            await webSocket.CloseAsync(
                WebSocketCloseStatus.MessageTooBig,
                closingMessage,
                cancellationToken);

            return closingMessage;
        }

        return Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
    }

    public static async Task SendMessageAsync(this WebSocket webSocket, string message,
        CancellationToken cancellationToken)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(message);
        var arraySegment = new ArraySegment<byte>(bytes);
        await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, cancellationToken);
    }
}