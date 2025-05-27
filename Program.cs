using System.Net.WebSockets;
using System.Text;
using websocket;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.UseWebSockets();


app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            var buffer = new byte[4096];

            // Receive username as first message
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var username = Encoding.UTF8.GetString(buffer, 0, result.Count);

            WebSocketHandler.Clients.Add(new ClientInfo { Socket = socket, Username = username });

            // Message loop
            while (true)
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    WebSocketHandler.Clients.RemoveAll(c => c.Socket == socket);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                var fullMessage = $"{username}: {message}";
                var messageBytes = Encoding.UTF8.GetBytes(fullMessage);

                var tasks = WebSocketHandler.Clients
                    .Where(c => c.Socket.State == WebSocketState.Open)
                    .Select(c => c.Socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None));

                await Task.WhenAll(tasks);
            }
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});


app.Run();
