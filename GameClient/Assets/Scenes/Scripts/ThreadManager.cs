using System;
using System.Collections.Generic;
using UnityEngine;

//This class contains functionality for multithreading - however, for our small game, we only need to run the program on the main thread.

public class ThreadManager : MonoBehaviour
{
    static readonly List<Action> mainThreadRun = new List<Action>();
    static readonly List<Action> mainThreadRunCopy = new List<Action>();
    static bool mainThreadRunAction = false;

    private void Update()
    {
        UpdateMainThread();
    }

//Prepares the function to be run on the main thread.
    public static void RunOnMainThread(Action a_inputs)
    {
        if (a_inputs == null)
        {
            Debug.Log("Nothing to run on main thread!");
            return;
        }

        lock (mainThreadRun)
        {
            mainThreadRun.Add(a_inputs);
            mainThreadRunAction = true;
        }
    }

//Runs the function on the main thread.
    public static void UpdateMainThread()
    {
        if (mainThreadRunAction)
        {
            mainThreadRunCopy.Clear();
            lock (mainThreadRun)
            {
                mainThreadRunCopy.AddRange(mainThreadRun);
                mainThreadRun.Clear();
                mainThreadRunAction = false;
            }

            for (int i = 0; i < mainThreadRunCopy.Count; i++)
            {
                mainThreadRunCopy[i]();
            }
        }
    }
}