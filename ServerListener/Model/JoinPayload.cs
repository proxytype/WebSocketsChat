using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerListener.Model
{
    public class JoinPayload
    {
        public string name { get; set; }
        public string sessionID { get; set; }
        public string ownerID { get; set; }
    }
}
