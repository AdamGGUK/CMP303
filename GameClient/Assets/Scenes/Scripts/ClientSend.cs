using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//This class sends data to the server.
public class ClientSend : MonoBehaviour
{
    private static void SendTCPData(Packet a_packet)
    {
        a_packet.WriteLength();
        Client.instance.tcp.SendData(a_packet);
    }

    private static void SendUDPData(Packet a_packet)
    {
        a_packet.WriteLength();
        Client.instance.udp.SendData(a_packet);
    }

    #region Packets
    public static void WelcomeReceived()
    {
        using (Packet a_packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            a_packet.Write(Client.instance.myId);
            a_packet.Write(UIManager.instance.usernameField.text);

            SendTCPData(a_packet);
        }
    }

    //Rather than simply sending a vector to the server, this function sends a bool array - and then the server determines movement.
    public static void PlayerMovement(bool[] a_inputs)
    {
        using (Packet a_packet = new Packet((int)ClientPackets.playerMovement))
        {
            a_packet.Write(a_inputs.Length);
            foreach (bool _input in a_inputs)
            {
                a_packet.Write(_input);
            }

            a_packet.Write(GameManager.players[Client.instance.myId].transform.rotation);

            SendUDPData(a_packet);
        }
    }
    #endregion
}