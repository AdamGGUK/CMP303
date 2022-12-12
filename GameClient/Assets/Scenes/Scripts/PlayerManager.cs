using UnityEngine;
using System.Diagnostics;

//Simple class that holds the player's member variables, and a few assorted timer functions.
public class PlayerManager : MonoBehaviour
{

    public int id;
    public string username;
    public Vector3 deltaVector;
    public Vector3 lastLastKnownPosition;
    public Vector3 lastKnownPosition;
    public Vector3 position;
    public Stopwatch timer;
    public float elapsedTime;
    public bool timerStarted;


    private void Update()
    {
      if (timerStarted == false)
        {
            timer = new Stopwatch();
            timer.Start();
            timerStarted = true;
        }
        elapsedTime = timer.ElapsedMilliseconds;
    }

    public void RestartTimer()
    {
        timer.Reset();
    }

    public float ReturnTime()
    {
        return elapsedTime;
    }
}
