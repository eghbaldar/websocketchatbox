using System.ComponentModel.DataAnnotations;

namespace websocket.Entities
{
    public class Users: BaseEntity
    {
        public string Fullname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Headshot { get; set; }
        public ICollection<UserSessions> UserSessions { get; set; }
    }
}
