using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class simply runs the WaitForPacket once per tick.
public class TimeChecker : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        ClientHandle.WaitForPacket();
    }
}
