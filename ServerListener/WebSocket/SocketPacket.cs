using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerListener.WebSocket
{
    public class SocketPacket
    {
        //websocket bit order - RFC6455
        //0 - FIN
        //1, 3 - RSV
        //4, 7 - OPERATION CODE
        //8 - MASK
        //9, 15 - PAYLOAD LENGTH
        //16, 32 - EXTENDED PAYLOAD

        public enum SOCKET_OPERATION {
            TEXT = 1,
            BINARY = 2,
            CLOSE = 8
        }

        public const int EXTENDED_PAYLOAD_LENGTH_127 = 127;
        public const int EXTENDED_PAYLOAD_LENGTH_126 = 126;
        public const int PAYLOAD_LENGTH_125 = 125;
        public const string SOCKET_METHOD_CREATE = "create";
        public const string SOCKET_METHOD_JOIN = "join";
        public const string SOCKET_METHOD_CHAT = "chat";
        public const string SOCKET_METHOD_MESSAGE = "message";

        public const int OFFSET_128 = 128;

        public bool fin { get; set; }
        public bool rsv1 { get; set; }
        public bool rsv2 { get; set; }
        public bool rsv3 { get; set; }
        public bool isMasked { get; set; }
        public int operation { get; set; }
        public ulong length { get; set; }
        public byte[] mask { get; set; }
        public byte[] payload { get; set; }
    }
}
