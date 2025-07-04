using Azure.Core;
using websocket.Context;

namespace websocket.Services.Messages.PostMessage
{
    public class PostMessageServiceDto
    {
        public string SenderUsername { get; set; }
        public string RecieverUsername { get; set; }
        public string Message { get; set; }
    }
    public class PostMessageService
    {
        private readonly IDataBaseContext _context;
        public PostMessageService(IDataBaseContext context)
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

            websocket.Entities.Messages messages = new websocket.Entities.Messages()
            {
                Message = req.Message,
                SenderId = sender.Id,
                RecieverId = reciever.Id,
            };
            _context.Messages.Add(messages);
            _context.SaveChanges();
            return true;
        }
    }
}
