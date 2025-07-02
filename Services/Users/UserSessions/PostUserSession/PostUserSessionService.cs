using websocket.Context;

namespace websocket.Services.Users.UserSessions.PostUserSession
{
    public class PostUserSessionService
    {
        private readonly IDataBaseContext _context;
        public PostUserSessionService(IDataBaseContext context)
        {
            _context = context;
        }
        public bool PostUserSession(Guid userId, string sessionValue)
        {
            if (userId == Guid.Empty || string.IsNullOrEmpty(sessionValue)) { return false; }
            var check = _context.Users.Any(x => x.Id == userId);
            if (!check) { return false; }

            // Revoke all previous sessions
            var sessions = _context.UserSessions.Where(x=>x.UserId == userId).ToList();
            foreach (var session in sessions) session.Active = false;
            
            // insert new one
            websocket.Entities.UserSessions userSessions = new Entities.UserSessions()
            {
                UserId = userId,
                SessionValue = sessionValue
            };
            _context.UserSessions.Add(userSessions);
            _context.SaveChanges();
            return true;
        }
    }
}
