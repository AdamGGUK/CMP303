using System;
using System.Net;
using UnityEngine;

//This class handles data received from the server.
public class ClientHandle : MonoBehaviour
{
    //Welcome message.
    public static void Welcome(Packet a_packet)
    {
        string _msg = a_packet.ReadString();
        int _myId = a_packet.ReadInt();

        UnityEngine.Debug.Log($"Message from server: {_msg}");
        Client.instance.myId = _myId;
        ClientSend.WelcomeReceived();

        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    public static void InstantiatePlayer(Packet a_packet)
    {
        //Spawns the player into the game, according to data contained in a packet.
        int _id = a_packet.ReadInt();
        string _username = a_packet.ReadString();
        Vector3 _position = a_packet.ReadVector();
        Quaternion _rotation = a_packet.ReadQuaternion();

        GameManager.instance.InstantiatePlayer(_id, _username, _position, _rotation);
    }

    public static void WaitForPacket()
    {
        //Check the player's internal timer. If the timer has passed a tick, then we haven't received a packet for a server tick - therefore, compensate for lag.
        foreach (PlayerManager player in GameManager.players.Values)
        {
            float elapsedTime = player.ReturnTime();

            if (elapsedTime > 0.34)
            {
                PlayerPosition(player.id);
                player.RestartTimer();
            }
        }
    }

    public static void PlayerPosition(Packet a_packet)
    {
        //Transforms gameobject according to packet data, and reset player's internal timer.
        int _id = a_packet.ReadInt();
        if (GameManager.players[_id].GetComponent<PlayerManager>().timerStarted)
        { 
        GameManager.players[_id].GetComponent<PlayerManager>().RestartTimer();
        }
        GameManager.players[_id].position = a_packet.ReadVector();

        GameManager.players[_id].transform.position = GameManager.players[_id].position;
        SetLastKnownPosition(_id, GameManager.players[_id].position);
    }


    public static void PlayerPosition(int a_id) 
    {
        //Lag compensation.
        
            GameManager.players[a_id].transform.position += GameManager.players[a_id].deltaVector;
    }

    public static void SetLastKnownPosition(int a_id, Vector3 a_vector)
    {
        //Set the player's last known position, so we can derive a delta vector from their current and last position.
        GameManager.players[a_id].lastLastKnownPosition = GameManager.players[a_id].lastKnownPosition;
        GameManager.players[a_id].lastKnownPosition = a_vector;
        GameManager.players[a_id].deltaVector = GameManager.players[a_id].lastKnownPosition - GameManager.players[a_id].lastLastKnownPosition;
    }

    public static void PlayerRotation(Packet a_packet)
    {
        //Rotates gameobject according to packet data.
        int _id = a_packet.ReadInt();
        Quaternion _rotation = a_packet.ReadQuaternion();

        GameManager.players[_id].transform.rotation = _rotation;
    }
}