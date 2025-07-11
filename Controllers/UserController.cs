﻿using Microsoft.AspNetCore.Mvc;
using websocket.Context;
using websocket.Services.Messages;
using websocket.Services.Users.Friendship;

namespace websocket.Controllers
{
    public class UserController : Controller
    {
        private readonly IDataBaseContext _context;
        public UserController(IDataBaseContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public JsonResult GetFriends(string username)
        {
            FriendshipService friendshipService = new FriendshipService(_context);
            var friends = friendshipService.GetFriends(username);
            return Json(friends);
        }
        public IActionResult PostMessage(PostMessageServiceDto req)
        {
            MessageService message = new MessageService(_context);
            if (message.PostMessage(req))
                return Json(new { IsSuccess = true });
            else return Json(new { IsSuccess = false });
        }
        public IActionResult GetMessages(
            string currentUsername,
            string friendUsername)
        {
            MessageService message = new MessageService(_context);
            return Json(message.GetMessagesByFriendUsername(currentUsername, friendUsername));
        }
        [HttpPost]
        public IActionResult PostFriend(
            string currentUsername,
            string friendUsername)
        {
            FriendshipService friendshipService = new FriendshipService(_context);
            if (friendshipService.AddFriend(currentUsername, friendUsername))
                return Json(new { IsSuccess = true });
            else return Json(new { IsSuccess = false });
        }
    }
}
