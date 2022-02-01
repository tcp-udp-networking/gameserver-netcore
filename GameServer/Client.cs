using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using UnityEngine;

namespace GameServer
{
    class Client
    {
        //public static Client instance;
        public static int dataBufferSize = 4096;

        public int id;
        public TCP tcp;
        public UDP udp;
        public Player player;
        public WebcamTexture webcamTexture;

        public Client(int _client)
        {
            id = _client;
            tcp = new TCP(id);
            udp = new UDP(id);
        }

        /// <summary>
        /// TCP Protocol
        /// </summary>
        public class TCP
        {
            public TcpClient socket;
            private readonly int id;
            public NetworkStream stream;
            private byte[] reciveBuffer;
            private Packet receivedData;

            public TCP(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receivedData = new Packet();
                reciveBuffer = new byte[dataBufferSize];

                stream.BeginRead(reciveBuffer, 0, dataBufferSize, RecieveCallback, null);

                ServerSend.Welcome(id, "Welcome to the server!");
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    if(_packet != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player with id: {id} via TCP: {_ex}");
                }
            }

            private void RecieveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead(_result);

                    if(_byteLength == 0)
                    {
                        Server.clients[id].Disconnect();
                        return;
                    }

                    byte[] _data = new byte[_byteLength];
                    Array.Copy(reciveBuffer, _data, _byteLength);

                    receivedData.Reset(HandleData(_data));
                    stream.BeginRead(reciveBuffer, 0, dataBufferSize, RecieveCallback, null);
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error receiving TCP data: {ex}");
                    Server.clients[id].Disconnect();
                }
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;
                receivedData.SetBytes(data);

                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet packet = new Packet(packetBytes))
                        {
                            int packetId = packet.ReadInt();
                            Server.packetHandlers[packetId](id, packet);
                        }
                    });

                    packetLength = 0;

                    if (receivedData.UnreadLength() >= 4)
                    {
                        packetLength = receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                return packetLength <= 1;
            }

            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receivedData = null;
                reciveBuffer = null;
                socket = null;
            }

        }

        /// <summary>
        /// UDP Protocol
        /// </summary>
        public class UDP
        {
            public UdpClient socket;
            public IPEndPoint endPoint;
            public int id;

            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
            }

            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }

            public void HandleData(Packet _packetData)
            {
                int packetLength = _packetData.ReadInt();
                byte[] packetBytes = _packetData.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using(Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        Server.packetHandlers[packetId](id, packet);
                    }
                });
            }

            public void Disconnect()
            {
                endPoint = null;
            }
        }


        public void SendIntoGame(string _playerName)
        {
           // var instanceTexture = new Texture2D(16, 16, TextureFormat.RGB24, false);
            player = new Player(id, _playerName, new System.Numerics.Vector3(0, 1f, 0), null);

            foreach (var client in Server.clients.Values)
            {
                if(client.player != null)
                {
                   if(client.id != id)
                   {
                       ServerSend.SpawnPlayer(id, client.player);
                   }
                }
            }


            foreach (var client in Server.clients.Values)
            {
                if (client.player != null)
                {
                    ServerSend.SpawnPlayer(client.id, player);
                }
            }
        }
        public void Disconnect()
        {
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} {player.username} has disconnected.");
            player = null;
            tcp.Disconnect();
            udp.Disconnect();
        }
    }
}
