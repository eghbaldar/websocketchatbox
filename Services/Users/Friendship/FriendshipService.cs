using websocket.Context;

namespace websocket.Services.Users.Friendship
{
    public class FriendshipService
    {
        private readonly IDataBaseContext _context;
        public FriendshipService(IDataBaseContext context)
        {
            _context = context;
        }
        public bool AreFriends(string user1_username, string user2_username)
        {
            var user1Id = _context.Users.FirstOrDefault(x => x.Username == user1_username);
            var user2Id = _context.Users.FirstOrDefault(x => x.Username == user2_username);
            if (user1Id == null) return false;
            if (user2Id == null) return false;

            return _context.Friends.Any(x => (x.User1 == user1Id.Id && x.User2 == user2Id.Id) ||
                    (x.User2 == user1Id.Id && x.User1 == user2Id.Id));
        }
    }
}