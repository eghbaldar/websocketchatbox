using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace websocket.Entities
{
    public class Conversations : BaseEntity
    {
        public Guid CreatorId { get; set; } //who did create the conversation?
        public Guid TargetId { get; set; } // who the messages are going to be sent?
    }
}
