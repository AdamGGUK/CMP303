using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

//Almost the same as the server-side "client" class.
public class Client : MonoBehaviour
{
    public static Client instance;
    public static int dataBufferSize = 4096;

    //What IP to connect to. Currently set to local network, and must be changed according to what network the server should run on.
    public string ip = "192.168.1.147";
    //It's important to pick a port that isn't already in use by another program.
    public int port = 26950;
    public int myId = 0;
    public TCP tcp;
    public UDP udp;

    private bool isConnected = false;
    private delegate void PacketHandler(Packet a_packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    public void ConnectToServer()
    {
        InitializeClientData();

        isConnected = true;
        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        public void Connect()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        private void ConnectCallback(IAsyncResult a_result)
        {
            socket.EndConnect(a_result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            receivedData = new Packet();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        public void SendData(Packet a_packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(a_packet.ToArray(), 0, a_packet.Length(), null, null);
                }
            }
            catch (Exception a_exception)
            {
                Debug.Log($"Error sending data to server via TCP: {a_exception}");
            }
        }

        private void ReceiveCallback(IAsyncResult a_result)
        {
            try
            {
                int _byteLength = stream.EndRead(a_result);
                if (_byteLength <= 0)
                {
                    instance.Disconnect();
                    return;
                }

                byte[] a_data = new byte[_byteLength];
                Array.Copy(receiveBuffer, a_data, _byteLength);

                receivedData.Reset(HandleData(a_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                Disconnect();
            }
        }

        private bool HandleData(byte[] a_data)
        {
            int a_packetLength = 0;

            receivedData.SetBytes(a_data);

            if (receivedData.UnreadLength() >= 4)
            {
                a_packetLength = receivedData.ReadInt();
                if (a_packetLength <= 0)
                {
                    return true;
                }
            }

            while (a_packetLength > 0 && a_packetLength <= receivedData.UnreadLength())
            {
                byte[] a_packetBytes = receivedData.ReadBytes(a_packetLength);
                ThreadManager.RunOnMainThread(() =>
                {
                    using (Packet a_packet = new Packet(a_packetBytes))
                    {
                        int a_packetId = a_packet.ReadInt();
                        packetHandlers[a_packetId](a_packet);
                    }
                });

                a_packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    a_packetLength = receivedData.ReadInt();
                    if (a_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (a_packetLength <= 1)
            {
                return true;
            }

            return false;
        }

        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int a_localPort)
        {
            socket = new UdpClient(a_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet a_packet = new Packet())
            {
                SendData(a_packet);
            }
        }

        public void SendData(Packet a_packet)
        {
            try
            {
                a_packet.InsertInt(instance.myId);
                if (socket != null)
                {
                    socket.BeginSend(a_packet.ToArray(), a_packet.Length(), null, null);
                }
            }

            catch (Exception a_exception)
            {
                Debug.Log($"Error sending data through UDP: {a_exception}");
            }
        }

        public void ReceiveCallback(IAsyncResult a_result)
        {
            try
            {
                byte[] a_data = socket.EndReceive(a_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (a_data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }

                instance.HandleData(a_data);
            }
            catch
            {
                Disconnect();
            }
        }

        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    public void HandleData(byte[] a_data)
    {
        using (Packet a_packet = new Packet(a_data))
        {
            int a_packetLength = a_packet.ReadInt();
            a_data = a_packet.ReadBytes(a_packetLength);
        }

        ThreadManager.RunOnMainThread(() =>
        {
            using (Packet a_packet = new Packet(a_data))
            {
                int a_packetId = a_packet.ReadInt();
                packetHandlers[a_packetId](a_packet);
            }
        });
    }


    public void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.InstantiatePlayer },
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition },
            { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation }
        };
        Debug.Log("Packets ready.");
    }

    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Disconnected.");
        }
    }
}