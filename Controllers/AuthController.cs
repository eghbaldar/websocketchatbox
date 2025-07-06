using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using websocket.Context;
using websocket.Services.Users.Authentication;
using websocket.Services.Users.UserSessions.PostUserSession;

namespace websocket.Controllers
{
    public class AuthController : Controller
    {
        private readonly IDataBaseContext _context;
        public AuthController(IDataBaseContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Login(string user, string pass)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass)) return BadRequest();

            CheckAuthentication auth = new CheckAuthentication(_context);
            var check = auth.Check(user, pass);
            if (check != Guid.Empty)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Only sent over HTTPS
                    SameSite = SameSiteMode.Strict, // Prevent CSRF
                    Expires = DateTimeOffset.UtcNow.AddDays(7) // Optional: set expiration
                };

                string secureValue = Guid.NewGuid().ToString();

                PostUserSessionService postUserSessionService = new PostUserSessionService(_context);
                postUserSessionService.PostUserSession(check, secureValue);

                Response.Cookies.Append("KingChatter", secureValue, cookieOptions);
                return Json(new { IsSuccess = true, Value = secureValue });
            }
            else
            {
                return Json(new { IsSuccess = false });
            }
        }
        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("KingChatter");
            return Redirect("/"); // or use Json if you're in SPA
        }

    }
}
