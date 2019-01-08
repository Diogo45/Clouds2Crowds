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

[UpdateAfter(typeof(AgentMovementSystem))]

public class VisualizationSystem : ComponentSystem
{
    [System.Serializable]
    public struct AgentRecord
    {
        public int AgentID;
        public float3 Position;
    }
    [System.Serializable]
    public class FrameRecord
    {
        public int frame;
        public List<AgentRecord> records;
    }

    public FrameRecord complete = new FrameRecord() { records = new List<AgentRecord>() };
    public FrameRecord processing = new FrameRecord() { records = new List<AgentRecord>() };

    public IReadOnlyList<AgentRecord> CurrentAgentPositions { get { return complete.records.AsReadOnly(); } }
    public int CurrentFrame { get { return complete.frame; } }

    public int frames = 0;

    public struct AgentGroup
    {
        public ComponentDataArray<Position> Position;
        [ReadOnly] public ComponentDataArray<AgentData> Data;
        [ReadOnly] public readonly int Length;
    }
    [Inject] public AgentGroup agentGroup;


    protected override void OnUpdate()
    {
        processing.records.Clear();
        processing.frame = frames++;

        for (int i = 0; i < agentGroup.Length; i++)
        {
            processing.records.Add(new AgentRecord
            {
                AgentID = agentGroup.Data[i].ID,
                Position = WindowManager.Crowds2Clouds(agentGroup.Position[i].Value)
            });
            
        }

        FrameRecord aux = complete;
        complete = processing;
        processing = aux;

    }
}
