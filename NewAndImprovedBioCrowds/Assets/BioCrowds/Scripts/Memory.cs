using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;
using Unity.Transforms;
using Unity.Jobs;
using UnityEditor;
using Unity.Rendering;

public struct MemoryData : IComponentData
{
    public int idNode;
}

[DisableAutoCreation]
[UpdateAfter(typeof(BioCrowds.CellTagSystem))]
public class DataGatheringSystem : JobComponentSystem
{

    [Inject] BioCrowds.CellTagSystem cellTagSystem;


    public NativeMultiHashMap<int, int> nearbyAgents;

    protected override void OnStartRunning()
    {
        nearbyAgents = new NativeMultiHashMap<int, int>(ControlVariables.instance.agentQuantity * ControlVariables.instance.agentQuantity, Allocator.Persistent);
    }


    public struct AgentGroup
    {
        [ReadOnly] public ComponentDataArray<Position> AgentPos;
        [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> AgentData;
        [ReadOnly] public EntityArray entity;
        [ReadOnly] public readonly int Length;
    }
    [Inject] public AgentGroup agentGroup;



    public struct FindNearbyAgents : IJobParallelFor
    {

        [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;

        [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> AgentData;

        [ReadOnly] public ComponentDataArray<Position> AgentPos;

        [WriteOnly] public NativeMultiHashMap<int, int>.Concurrent nearbyAgents;
        [ReadOnly] public EntityArray entity;



        public void Execute(int index)
        {

            int myID = AgentData[index].ID;
            float3 myPos = AgentPos[index].Value;

            int i = 0;
            float3 agentPos;

            while (AgentIDToPos.TryGetValue(i, out agentPos))
            {
                if (i == myID)
                {
                    i++;

                    continue;
                }

                float dist = math.distance(myPos, agentPos);

                if (dist < 1f)
                {
                    nearbyAgents.Add(myID, i);
                }
                
                
                

                i++;

            }






        }
    }


    protected override void OnStopRunning()
    {
        nearbyAgents.Dispose();
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {


        nearbyAgents.Clear();

        FindNearbyAgents findNearbyAgentsJob = new FindNearbyAgents
        {
            AgentIDToPos = cellTagSystem.AgentIDToPos,
            AgentData = agentGroup.AgentData,
            AgentPos = agentGroup.AgentPos,
            nearbyAgents = nearbyAgents.ToConcurrent()
        };

        JobHandle jobHandle = findNearbyAgentsJob.Schedule(agentGroup.Length, 1, inputDeps);

        jobHandle.Complete();



        for (int i = 0; i < agentGroup.Length; i++)
        {
            int key = agentGroup.AgentData[i].ID;
            int item;
            NativeMultiHashMapIterator<int> it;
            if (nearbyAgents.TryGetFirstValue(key, out item, out it))
            {
                Debug.Log(key + " --> " + item);
            }

        }




        return jobHandle;

    }


}