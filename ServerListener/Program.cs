using ServerListener.Sockets;
using ServerListener.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServerListener
{
    class Program
    {



        private static TcpListener listener = null;
        private static SessionManager sessionManager = null;


        private static void createHost(string host, int port)
        {




            try
            {
                sessionManager = new SessionManager();

                listener = new TcpListener(IPAddress.Parse(host), port);
                listener.Start();
                writeConsole("Listener Start:" + host + ":" + port);

                while (true)
                {

                    TcpClient client = listener.AcceptTcpClient();

                    writeConsole("Client Connected:" + (client.Client.LocalEndPoint as IPEndPoint).Address.ToString());

                    sessionManager.createSession(client);

                }




            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }



        }



        

        private static string handleRequest(NetworkStream stream, int length)
        {
            byte[] bytes = new byte[length];
            stream.Read(bytes, 0, length);

            return Encoding.UTF8.GetString(bytes);
        }


        private static void writeConsole(string message)
        {
            Console.WriteLine("#" + DateTime.Now.ToShortDateString() + ": " + message);
        }


        static void Main(string[] args)
        {
            createHost("127.0.0.1", 777);
        }


    }
}
