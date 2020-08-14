using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerListener.WebSocket
{
    public class SocketResponse
    {
        public bool isValid { get; set; }
        public object payload { get; set; }
        public string method { get; set; }
        public SocketResponse()
        {
            isValid = true;
        }
    }
}
