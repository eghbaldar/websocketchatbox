using Microsoft.AspNetCore.Mvc;
using websocket.Context;
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
    }
}
