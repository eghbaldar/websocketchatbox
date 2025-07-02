using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace websocket.Entities
{
    public class FriendHidden : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid FriendId { get; set; }
    }
}
