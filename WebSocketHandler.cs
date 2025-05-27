using System.Net.WebSockets;

namespace websocket
{
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
