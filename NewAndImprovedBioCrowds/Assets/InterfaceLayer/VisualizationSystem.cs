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

public struct BioCrowdsAnchor : IComponentData
{
    public float3 Pivot;
}
[UpdateAfter(typeof(BioCrowds.AgentMovementSystem))]
public class BioCrowdsPivotCorrectonatorSystemDeluxe : JobComponentSystem
{
    private float3 currentPivot;
    private bool dirtyFlag;

    public void PivotChange(float3 newPivot)
    {
        currentPivot = newPivot;
        dirtyFlag = true;
    }
    public struct AnchorCorrectGroup
    {
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<BioCrowdsAnchor> Pivot;
        [ReadOnly] public readonly int Length;
    };

    [Inject]public AnchorCorrectGroup m_AnchorGroup;
    
    public struct PositionCorrectonatorJob : IJobParallelFor
    {
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<BioCrowdsAnchor> Pivot;
        [ReadOnly] public float3 newPivot;

        public void Execute(int index)
        {
            float3 oldPos = Position[index].Value;
            float3 oldPivot = Pivot[index].Pivot;

            float3 newPos = WindowManager.ChangePivot(oldPos, oldPivot, newPivot);

            //Debug.Log("Pivots : " + newPivot + oldPivot + " Positions: " + oldPos + newPos);

            Position[index] = new Position { Value = newPos };
            Pivot[index] = new BioCrowdsAnchor { Pivot = newPivot };
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!dirtyFlag)
            return inputDeps;

        var job = new PositionCorrectonatorJob
        {
            Position = m_AnchorGroup.Position,
            Pivot = m_AnchorGroup.Pivot,
            newPivot = currentPivot
        };

        var jobHandle = job.Schedule(m_AnchorGroup.Length, 1, inputDeps);
        dirtyFlag = false;
        return jobHandle;
    }
}

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
