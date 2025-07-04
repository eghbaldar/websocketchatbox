using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using websocket.Context;
using websocket.Global;
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
                    string? fullname = null;

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
                            var _user = _dbContext.Users.Where(x => x.Id == user.UserId).First();
                            username = _user.Username;
                            fullname = _user.Fullname;
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
                        await socket.SendAsync(Encoding.UTF8.GetBytes($"Authenticated:{username}:{fullname}"), WebSocketMessageType.Text, true, CancellationToken.None);

                        //GeneralStatics.ThisUsername = username;
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

                            var msgText = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

                            if (msgText.StartsWith("{") && msgText.Contains("\"type\""))
                            {
                                using var doc = JsonDocument.Parse(msgText);
                                var root = doc.RootElement;

                                var type = root.GetProperty("type").GetString();

                                if (type == "typing")
                                {
                                    // Broadcast typing to all friends
                                    var typingPayload = $"{username}: {root}";
                                    var typingBytes = Encoding.UTF8.GetBytes(typingPayload);

                                    var friendshipService = new FriendshipService(_dbContext);

                                    var targets = WebSocketHandler.Clients
                                        .Where(c => c.Socket.State == WebSocketState.Open &&
                                                    c.Username != username &&
                                                    friendshipService.AreFriends(username, c.Username))
                                        .ToList();

                                    foreach (var target in targets)
                                    {
                                        await target.Socket.SendAsync(new ArraySegment<byte>(typingBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                    }

                                    continue;
                                }
                                else if (type == "chat")
                                {
                                    var recipient = root.GetProperty("to").GetString();
                                    var message = root.GetProperty("message").GetString();

                                    var fullMessage = $"{username}: {message}";
                                    var messageBytes = Encoding.UTF8.GetBytes(fullMessage);

                                    var friendshipService = new FriendshipService(_dbContext);

                                    var targetClient = WebSocketHandler.Clients
                                        .FirstOrDefault(c => c.Username == recipient &&
                                                             c.Socket.State == WebSocketState.Open &&
                                                             friendshipService.AreFriends(username, recipient));

                                    if (targetClient != null)
                                    {
                                        await targetClient.Socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                    }

                                    // Echo to sender
                                    await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                                    continue;
                                }
                            }

                            // ✉️ Fallback: normal (non-structured) message broadcast to all friends
                            var fallbackMessage = $"{username}: {msgText}";
                            var fallbackBytes = Encoding.UTF8.GetBytes(fallbackMessage);

                            var fallbackFriendshipService = new FriendshipService(_dbContext);

                            var fallbackTargets = WebSocketHandler.Clients
                                .Where(c => c.Socket.State == WebSocketState.Open &&
                                            c.Username != username &&
                                            fallbackFriendshipService.AreFriends(username, c.Username))
                                .ToList();

                            foreach (var target in fallbackTargets)
                            {
                                await target.Socket.SendAsync(new ArraySegment<byte>(fallbackBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                            }

                            // Optional echo to sender
                            await socket.SendAsync(new ArraySegment<byte>(fallbackBytes), WebSocketMessageType.Text, true, CancellationToken.None);
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
        //public string fullname { get; set; }
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
