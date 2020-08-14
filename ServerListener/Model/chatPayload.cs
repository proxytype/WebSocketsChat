using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerListener.Model
{
    public class chatPayload
    {
        public string ownerID { get; set; }
        public string name { get; set; }
        public string sessionID { get; set; }
        public string message { get; set; }
    }
}
