using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Text;
using websocket;
using websocket.Context;
using websocket.Helpers;
using websocket.Services.Messages;
using websocket.Services.Users.Friendship;
using websocket.Services.Users.UserSessions.PostUserSession;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IDataBaseContext,DataBaseContext>();
builder.Services.AddScoped<FriendshipService>();
builder.Services.AddScoped<PostUserSessionService>();
builder.Services.AddScoped<MessageService>();

var ConStr = builder.Configuration.GetConnectionString("LocalServer");
builder.Services.AddEntityFrameworkSqlServer().AddDbContext<DataBaseContext>(x => x.UseSqlServer(ConStr));

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


app.UseMiddleware<Conductor>();
app.Run();
