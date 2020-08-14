using Newtonsoft.Json;
using ServerListener.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ServerListener.WebSocket
{
    public class SessionManager
    {
        Dictionary<string, SessionWorker> workers = null;

        public SessionManager()
        {
            workers = new Dictionary<string, SessionWorker>();
        }

        public void createSession(TcpClient client)
        {

            SessionWorker worker = new SessionWorker(client);
            worker.packetCallback += Worker_packetCallback;
            workers.Add(worker.sessionID, worker);

        }

        private void Worker_packetCallback(string sessionId, string data)
        {
            try
            {
                SocketRequest socketRequest = JsonConvert.DeserializeObject<SocketRequest>(data);
                SocketResponse response = new SocketResponse();

                SessionWorker worker = workers[sessionId];
                response.method = socketRequest.method;

                if (socketRequest.method == SocketPacket.SOCKET_METHOD_CREATE)
                {
                    string name = socketRequest.payload.ToString();

                    worker.setName(name);
                    Console.WriteLine("Session:" + sessionId + " Created By:" + name);
                    response.payload = new JoinPayload() { sessionID = sessionId, name = worker.name, ownerID = worker.ownerID };
                    writeWorker(response, worker);
                }
                else if (socketRequest.method == SocketPacket.SOCKET_METHOD_JOIN)
                {
                    JoinPayload joinPayload = JsonConvert.DeserializeObject<JoinPayload>(socketRequest.payload.ToString());
                    if (workers.ContainsKey(joinPayload.sessionID))
                    {
                        worker.setName(joinPayload.name);

                        sendBroadCast(workers[joinPayload.sessionID], new SocketResponse()
                        {
                            isValid = true,
                            payload = new chatPayload()
                            {
                                message = joinPayload.name + " Join Session",
                                ownerID = workers[joinPayload.sessionID].ownerID,
                                sessionID = workers[joinPayload.sessionID].sessionID
                            },
                            method = SocketPacket.SOCKET_METHOD_MESSAGE
                        });

                        workers[joinPayload.sessionID].setJoinPayload(new JoinPayload()
                        {
                            name = joinPayload.name,
                            ownerID = worker.ownerID,
                            sessionID = worker.sessionID
                        });

                        Console.WriteLine("Session:" + joinPayload.sessionID + " Join:" + joinPayload.name);
                        response.payload = new JoinPayload()
                        {
                            sessionID = joinPayload.sessionID,
                            name = worker.name,
                            ownerID = worker.ownerID
                        };
                    }
                    else
                    {
                        response.isValid = false;
                        response.payload = "Session not exists";
                    }

                    writeWorker(response, worker);
                }
                else if (socketRequest.method == SocketPacket.SOCKET_METHOD_CHAT)
                {
                    chatPayload chatPayload = JsonConvert.DeserializeObject<chatPayload>(socketRequest.payload.ToString());

                    if (workers.ContainsKey(chatPayload.sessionID))
                    {
                        response.method = SocketPacket.SOCKET_METHOD_MESSAGE;
                        chatPayload chat = new chatPayload()
                        {
                            sessionID = chatPayload.sessionID,
                            name = chatPayload.name,
                            ownerID = chatPayload.ownerID,
                            message = chatPayload.message
                        };

                        sendBroadCast(workers[chatPayload.sessionID], new SocketResponse()
                        {
                            isValid = true,
                            payload = chat,
                            method = SocketPacket.SOCKET_METHOD_MESSAGE
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void writeWorker(SocketResponse response, SessionWorker worker)
        {
            string payloadStr = JsonConvert.SerializeObject(response);
            worker.writeClient(payloadStr);
        }

        public void sendBroadCast(SessionWorker worker, SocketResponse response)
        {
            string payloadStr = JsonConvert.SerializeObject(response);
            byte[] payload = Encoding.UTF8.GetBytes(payloadStr);

            worker.writeClient(payloadStr);

            foreach (string key in worker.users.Keys)
            {
                workers[worker.users[key].sessionID].writeClient(payloadStr);
            }

        }

    }
}
