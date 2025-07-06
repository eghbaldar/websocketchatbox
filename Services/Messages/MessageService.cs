using Azure.Core;
using websocket.Context;

namespace websocket.Services.Messages
{
    // Post Messages
    public class PostMessageServiceDto
    {
        public string SenderUsername { get; set; }
        public string RecieverUsername { get; set; }
        public string Message { get; set; }
    }
    // Get Messages
    public class GetMessageServiceDto
    {
        public Guid SenderId { get; set; }
        public Guid RecieverId { get; set; }
        public string SenderUsername { get; set; }
        public string? SenderHeadshot { get; set; }
        public string RecieverUsername { get; set; }
        public string? RecieverHeadshot { get; set; }
        public string Message { get; set; }
        public DateTime InsertDate { get; set; }
    }
    public class ResultGetMessageServiceDto
    {
        public List<GetMessageServiceDto> Result { get; set; }
    }
    // Service
    public class MessageService
    {
        private readonly IDataBaseContext _context;
        public MessageService(IDataBaseContext context)
        {
            _context = context;
        }
        public bool PostMessage(PostMessageServiceDto req)
        {
            if (string.IsNullOrEmpty(req.Message) || req == null) return false;

            var sender = _context.Users.FirstOrDefault(x => x.Username == req.SenderUsername);
            if (sender == null) return false;
            var reciever = _context.Users.FirstOrDefault(x => x.Username == req.RecieverUsername);
            if (reciever == null) return false;

            Entities.Messages messages = new Entities.Messages()
            {
                Message = req.Message,
                SenderId = sender.Id,
                RecieverId = reciever.Id,
            };
            _context.Messages.Add(messages);
            _context.SaveChanges();
            return true;
        }
        public ResultGetMessageServiceDto GetMessagesByFriendUsername(
            string currentUsername,
            string friendUsername)
        {
            var current = _context.Users.FirstOrDefault(x => x.Username == currentUsername);
            if (current == null) return null;
            var friend = _context.Users.FirstOrDefault(x => x.Username == friendUsername);
            if (friend == null) return null;

            var messages = _context.Messages
                .Where(x =>
                (x.SenderId == current.Id && x.RecieverId == friend.Id)
                ||
                (x.SenderId == friend.Id && x.RecieverId == current.Id)
                )
                .Select(x => new GetMessageServiceDto
                {
                    SenderId = x.SenderId,
                    RecieverId = x.RecieverId,
                    InsertDate = x.InsertDate,
                    Message = x.Message,
                    SenderUsername = (x.SenderId == current.Id) ? currentUsername : friendUsername,
                    RecieverUsername = (x.SenderId == current.Id) ? currentUsername : friendUsername,
                    SenderHeadshot = _context.Users.FirstOrDefault(y => y.Id == x.SenderId).Headshot,
                    RecieverHeadshot = _context.Users.FirstOrDefault(y => y.Id == x.RecieverId).Headshot,
                })
                .OrderBy(x => x.InsertDate)
                .ToList();
            return new ResultGetMessageServiceDto
            {
                Result = messages
            };
        }
    }
}
