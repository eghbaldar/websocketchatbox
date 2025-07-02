using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using websocket.Context;
using websocket.Services.Users.Authentication;
using websocket.Services.Users.Friendship;

namespace websocket.Helpers
{
    public class Conductor
    {
        private readonly RequestDelegate _next;

        public Conductor(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IDataBaseContext _dbContext)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var socket = await context.WebSockets.AcceptWebSocketAsync();
                    var buffer = new byte[4096];
                    string? username = null;

                    try
                    {
                        // 🔐 Try to authenticate using cookie
                        if (context.Request.Cookies.TryGetValue("KingChatter", out var sessionToken))
                        {
                            var user = _dbContext.UserSessions
                                .FirstOrDefault(u => u.SessionValue == sessionToken && u.Expiration >= DateTime.UtcNow && u.Active);
                            if (user == null)
                            {
                                await SendAndClose(socket, "Authentication failed (invalid cookie)");
                                return;
                            }
                            var _username = _dbContext.Users.Where(x => x.Id == user.UserId).First().Username;
                            username = _username;
                        }
                        else
                        {
                            // 🔑 Fall back to receiving credentials via WebSocket
                            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            var jsonString = Encoding.UTF8.GetString(buffer, 0, result.Count);

                            var credentials = JsonSerializer.Deserialize<AuthPayload>(jsonString);
                            if (credentials == null)
                            {
                                await SendAndClose(socket, "Invalid authentication format");
                                return;
                            }

                            var auth = new CheckAuthentication(_dbContext);
                            if (auth.Check(credentials.username, credentials.password) != Guid.Empty)
                            {
                                await SendAndClose(socket, "Authentication failed");
                                return;
                            }

                            username = credentials.username;
                        }

                        // ✅ Success
                        await socket.SendAsync(Encoding.UTF8.GetBytes("Authenticated"), WebSocketMessageType.Text, true, CancellationToken.None);

                        WebSocketHandler.Clients.Add(new ClientInfo
                        {
                            Socket = socket,
                            Username = username
                        });

                        // 💬 Start chat loop
                        while (socket.State == WebSocketState.Open)
                        {
                            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            if (result.MessageType == WebSocketMessageType.Close)
                                break;

                            var msg = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

                            // 🛡️ Skip credentials sent again
                            if (msg.StartsWith("{") && msg.Contains("username") && msg.Contains("password"))
                                continue;

                            var fullMessage = $"{username}: {msg}";
                            var messageBytes = Encoding.UTF8.GetBytes(fullMessage);

                            FriendshipService friendshipService = new FriendshipService(_dbContext);

                            var targets = WebSocketHandler.Clients
                                .Where(c => c.Socket.State == WebSocketState.Open &&
                                            c.Username != username &&
                                            friendshipService.AreFriends(username,c.Username))
                                .ToList();

                            foreach (var target in targets)
                            {
                                await target.Socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                            }

                            // 🔁 Optional echo to self
                            await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Socket error: " + ex.Message);
                    }
                    finally
                    {
                        WebSocketHandler.Clients.RemoveAll(c => c.Socket == socket);
                        if (socket.State == WebSocketState.Open)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task SendAndClose(WebSocket socket, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, message, CancellationToken.None);
        }
    }

    public class AuthPayload
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public static class WebSocketHandler
    {
        public static List<ClientInfo> Clients { get; } = new List<ClientInfo>();
    }

    public class ClientInfo
    {
        public WebSocket Socket { get; set; }
        public string Username { get; set; }
    }
}
