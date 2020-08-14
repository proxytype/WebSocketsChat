using ServerListener.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServerListener.WebSocket
{
    public class SessionWorker
    {
        public delegate void ProcessPacketCallBack(string sessionId, string data);
        public event ProcessPacketCallBack packetCallback;

        public bool isRunning = false;
        public bool isHandShake = true;

        public string sessionID = string.Empty;
        public string name = string.Empty;
        public string ownerID = string.Empty;

        private Random Randomizer = null;
        private TcpClient client;
        private BackgroundWorker worker;
        private List<SocketPacket> packets = null;
        public Dictionary<string, JoinPayload> users = null;

        public SessionWorker(TcpClient _client)
        {
            sessionID = generateSessionID();

            Randomizer = new Random();
            client = _client;

            packets = new List<SocketPacket>();
            users = new Dictionary<string, JoinPayload>();

            worker = new BackgroundWorker();

            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync();

        }

        public void setName(string _name)
        {
            name = _name;
            ownerID = generateSessionID();
        }

        public void setJoinPayload(JoinPayload payload)
        {
            if (!users.ContainsKey(payload.ownerID))
            {
                users.Add(payload.ownerID, payload);
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            isRunning = false;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            isRunning = true;

            NetworkStream stream = client.GetStream();

            while (true)
            {
                if (stream.DataAvailable)
                {
                    if (isHandShake)
                    {
                        if (makeHandShake(ref stream))
                        {
                            isHandShake = false;
                        }
                    }
                    else
                    {
                        SocketPacket packet = new SocketPacket();

                        if (readPacket(ref stream, ref packet))
                        {
                            packets.Add(packet);

                            if (packet.fin)
                            {
                                processPacket(ref stream);
                                packets.Clear();
                            }
                        }
                    }
                }
                else {
                    Thread.Sleep(100);
                }
            }
        }

        public bool writeClient(string payload)
        {

            try
            {
                NetworkStream stream = client.GetStream();

                Queue<string> queue = new Queue<string>(splitString(payload, SocketPacket.PAYLOAD_LENGTH_125));
                int len = queue.Count;

                while (queue.Count > 0)
                {
                    int finalFrame = 1; //fin: 0 = more frames, 1 = final frame
                    if (queue.Count > 1) {
                        finalFrame = 0;
                    }

                    int continueFrame = 1;
                    if (queue.Count != len) {
                        continueFrame = 0; //opcode : 0 = continuation frame, 1 = text
                    }

                    finalFrame = (finalFrame << 1) + 0;//rsv1
                    finalFrame = (finalFrame << 1) + 0;//rsv2
                    finalFrame = (finalFrame << 1) + 0;//rsv3
                    finalFrame = (finalFrame << 4) + continueFrame;
                    finalFrame = (finalFrame << 1) + 0; //mask: server -> client = no mask

                    byte[] list = Encoding.UTF8.GetBytes(queue.Dequeue());
                    finalFrame = (finalFrame << 7) + list.Length;
                    stream.Write(intToBytes((ushort)finalFrame), 0, 2);
                    stream.Write(list, 0, list.Length);
                }

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private byte[] intToBytes(ushort value)
        {
            byte[] array = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(array);
            }

            return array;
        }

        private string[] splitString(string original, int size)
        {

            List<string> stack = new List<string>();
            int end = original.Length;

            int counter = 0;

            while (end - counter > size)
            {
                stack.Add(original.Substring(counter, size));
                counter = counter + size;
            }

            stack.Add(original.Substring(counter));

            return stack.ToArray();
        }

        private bool readPacket(ref NetworkStream stream, ref SocketPacket packet)
        {
            try
            {

                //first byte handling
                int firstByte = stream.ReadByte();

                packet.fin = (firstByte & (1 << 7)) != 0;

                packet.rsv1 = (firstByte & (1 << 6)) != 0;
                packet.rsv2 = (firstByte & (1 << 5)) != 0;
                packet.rsv3 = (firstByte & (1 << 4)) != 0;

                packet.operation = firstByte & ((1 << 4) - 1);

                int secondByte = stream.ReadByte();

                packet.isMasked = (secondByte & (1 << 7)) != 0;

                int length = secondByte;

                if (packet.isMasked)
                {
                    length = length - SocketPacket.OFFSET_128;
                }

                if (length > 0 && length < SocketPacket.EXTENDED_PAYLOAD_LENGTH_126)
                {
                    packet.length = (ulong)length;
                }
                else
                {

                    byte[] tempLength = null;
                    //big packet
                    if (length == SocketPacket.EXTENDED_PAYLOAD_LENGTH_126)
                    {
                        tempLength = new byte[2];
                        stream.Read(tempLength, 0, tempLength.Length);
                        Array.Reverse(tempLength);
                        packet.length = (ulong)BitConverter.ToInt16(tempLength, 0);
                    }
                    //biggest packet
                    else if (length == SocketPacket.EXTENDED_PAYLOAD_LENGTH_127)
                    {
                        tempLength = new byte[8];
                        stream.Read(tempLength, 0, tempLength.Length);
                        Array.Reverse(tempLength);
                        packet.length = (ulong)BitConverter.ToInt64(tempLength, 0);
                    }


                }

                packet.mask = new byte[4];
                if (packet.isMasked)
                {
                    stream.Read(packet.mask, 0, packet.mask.Length);
                }

                packet.payload = new byte[packet.length];
                stream.Read(packet.payload, 0, packet.payload.Length);

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }

        private bool processPacket(ref NetworkStream stream)
        {

            try
            {
                string data = string.Empty;

                for (int i = 0; i < packets.Count; i++)
                {
                    SocketPacket packet = packets[i];

                    if (packet.operation == 1)
                    {
                        if (packet.isMasked)
                        {
                            byte[] decoded = setMask(packet.payload, packet.mask);
                            data = data + Encoding.UTF8.GetString(decoded);
                        }
                    }
                }

                packetCallback?.Invoke(sessionID, data);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool makeHandShake(ref NetworkStream stream)
        {
            try
            {
                stream = client.GetStream();
                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, client.Available);
                string request = Encoding.UTF8.GetString(bytes);

                string swk = Regex.Match(request, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                byte[] response = Encoding.UTF8.GetBytes(
                           "HTTP/1.1 101 Switching Protocols\r\n" +
                           "Connection: Upgrade\r\n" +
                           "Upgrade: websocket\r\n" +
                           "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private byte[] setMask(byte[] payload, byte[] mask)
        {
            byte[] decoded = new byte[payload.Length];
            for (var i = 0; i < payload.Length; i++)
                decoded[i] = (byte)(payload[i] ^ mask[i % 4]);
            return decoded;
        }

        private string generateSessionID()
        {
            //https://stackoverflow.com/questions/11313205/generate-a-unique-id
            StringBuilder builder = new StringBuilder();
            Enumerable
               .Range(65, 26)
                .Select(e => ((char)e).ToString())
                .Concat(Enumerable.Range(97, 26).Select(e => ((char)e).ToString()))
                .Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
                .OrderBy(e => Guid.NewGuid())
                .Take(11)
                .ToList().ForEach(e => builder.Append(e));
            string id = builder.ToString();
            return id;

        }

    }
}
