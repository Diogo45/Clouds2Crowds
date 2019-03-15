using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using BioCrowds;
using System.IO;

[UpdateBefore(typeof(BioCities.CloudTagDesiredQuantitySystem))]
public class FrameCounter : ComponentSystem {

    public float currentTime;


    protected override void OnUpdate()
    {
        currentTime = Time.realtimeSinceStartup;
    }
    

    protected override void OnCreateManager()
    {
        base.OnCreateManager();        
    }

}



[UpdateAfter(typeof(BioCrowds.DespawnAgentBarrier))]
public class EndFrameCounter : ComponentSystem
{

    public List<float> frameTimes;
    string filename;
    [Inject] public FrameCounter m_frameCounterSystem;

    protected override void OnUpdate()
    {
        float t = Time.realtimeSinceStartup - m_frameCounterSystem.currentTime;
        frameTimes.Add(t);
        filename = BioCities.Parameters.Instance.LogFilePath + "FrameTimes.txt";
        //Debug.Log(t);
    }


    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        frameTimes = new List<float>();
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
        
        using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(filename))
        {
            foreach(float f in frameTimes)
            {
                
                file.WriteLine(f);
            }
        }

        Debug.Log("Recorded");
    }

}