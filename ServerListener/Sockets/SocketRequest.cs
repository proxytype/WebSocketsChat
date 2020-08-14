using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerListener.Sockets
{
    public class SocketRequest
    {
        public string method { get; set; }
        public string payload { get; set; }
        public SocketRequest()
        {
            //
            // TODO: Add constructor logic here
            //
        }
    }
}
