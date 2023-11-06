using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace WebServer
{
    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSocketHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<WebSocketMiddleware>();
        }
    }

    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<Guid, WebSocket> _connectedClients = new ConcurrentDictionary<Guid, WebSocket>();
        private static readonly List<string> _receivedMessages = new List<string>();

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
                Guid clientId = Guid.NewGuid();
                string clientName = context.Request.Query["name"];

                _connectedClients.TryAdd(clientId, socket);

                while (socket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.CloseStatus.HasValue)
                    {
                        _connectedClients.TryRemove(clientId, out _);
                        await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    _receivedMessages.Add(string.Format("{0} ({1}): {2}", clientName, DateTime.Now.ToString("T"), message));
                }
            }
            else
            {
                await _next(context);
            }
        }

        public static string GetReceivedMessages()
        {
            string messages = string.Join("<br/>", _receivedMessages.ToArray());
            return messages;
        }

        public static async Task BroadcastMessage(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            foreach (WebSocket client in _connectedClients.Values)
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }

            _receivedMessages.Add(string.Format("Server: ({0}): {1}", DateTime.Now.ToString("T"), message));
        }
    }
}
