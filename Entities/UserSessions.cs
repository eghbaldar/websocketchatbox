namespace websocket.Entities
{
    public class UserSessions: BaseEntity
    {
        public Guid UserId { get; set; }
        public virtual Users User { get; set; }
        public string SessionValue { get; set; }
        public bool Active { get; set; } = true;
        public DateTime Expiration { get; set; } = DateTime.UtcNow.AddDays(1);
    }
}
