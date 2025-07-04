using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace websocket.Entities
{
    public class Messages : BaseEntity
    {
        public Guid SenderId { get; set; }
        public bool SenderSeen { get; set; }
        public bool SenderDelete { get; set; }

        public Guid RecieverId { get; set; }
        public bool RecieverSeen { get; set; }
        public bool RecieverDelete { get; set; }

        public string Message { get; set; }
        public bool Buzz { get; set; } // ture: this message is only buzz
    }
}
