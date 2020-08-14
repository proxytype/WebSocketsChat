using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerListener.WebSocket
{
    public class SocketRequest
    {
        public string method { get; set; }
        public object payload { get; set; }
        public SocketRequest()
        {
            //
            // TODO: Add constructor logic here
            //
        }
    }
}
