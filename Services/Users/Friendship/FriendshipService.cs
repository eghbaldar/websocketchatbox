using Microsoft.AspNetCore.Mvc;
using websocket.Context;
using websocket.Entities;

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
        
        //////////////////////////////////////////
        //// Get Friends        
        //////////////////////////////////////////
        public class FriendsDto
        {
            public Guid FriendId { get; set; }
            public string FriendUsername { get; set; }
            public string FriendName { get; set; }
            public string? FriendHeadshot { get; set; }
        }
        public class ResultFriendsDto
        {
            public bool IsSuccess { get; set; }
            public List<FriendsDto> Result { get; set; }
        }
        public ResultFriendsDto GetFriends(string username)
        {
            var user = _context.Users.FirstOrDefault(x => x.Username == username);
            if (user == null)
            {
                return new ResultFriendsDto
                {
                    IsSuccess = false,
                };
            }

            var friendIds = _context.Friends
                .Where(f => f.User1 == user.Id || f.User2 == user.Id)
                .Select(f => f.User1 == user.Id ? f.User2 : f.User1)
                .ToList();

            var friends = _context.Users
                .Where(u => friendIds.Contains(u.Id))
                .Select(u => new FriendsDto
                {
                    FriendId = u.Id,
                    FriendName = u.Fullname,
                    FriendUsername = u.Username,
                    FriendHeadshot = u.Headshot,
                })
                .ToList();

            return new ResultFriendsDto
            {
                IsSuccess = true,
                Result = friends,
            };
        }
        //////////////////////////////////////////
        //// Add a Friend
        //////////////////////////////////////////
        public bool AddFriend(string currentUsername, string friendUsername)
        {
            if (AreFriends(currentUsername, friendUsername)) return false;
            var user = _context.Users.FirstOrDefault(x => x.Username == currentUsername);
            var friend = _context.Users.FirstOrDefault(x => x.Username == friendUsername);
            if (user == null) return false;
            if (friend == null) return false;

            Friends friends = new Friends()
            {
                User1 = user.Id,
                User2 = friend.Id,
            };
            _context.Friends.Add(friends);
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
    }
}