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


public static class MemoryBufferLength
{
    public const int length = 500;
}

[InternalBufferCapacity(MemoryBufferLength.length)]
public struct AgentElement : IBufferElementData
{
    public static implicit operator InteractionData(AgentElement e) { return e.interaction; }
    public static implicit operator AgentElement(InteractionData e) { return new AgentElement { interaction = e }; }
    public InteractionData interaction;
}


public struct InteractionData
{
    public int AgentID;
    public int numFrames;
}

[UpdateAfter(typeof(AddInteractionBuffer))]
[UpdateBefore(typeof(InitializeInteractionBuffer))]
public class AddInteractionBufferBarrier : BarrierSystem { }

[UpdateBefore(typeof(AddInteractionBufferBarrier))]
public class AddInteractionBuffer : JobComponentSystem
{
    [Inject] AddInteractionBufferBarrier barrier;



    public struct AgentGroup
    {
        public ComponentDataArray<BioCrowds.AgentData> agentData;

        [ReadOnly] public EntityArray Entities;
        [ReadOnly] public readonly int Length;

    }

    [Inject] AgentGroup agentGroup;


    public struct AddBufferJob : IJobParallelFor
    {

        public EntityCommandBuffer.Concurrent commandBuffer;
        public EntityArray entityArray;

        public void Execute(int index)
        {
            commandBuffer.AddBuffer<AgentElement>(index, entityArray[index]);
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var addbufferJob = new AddBufferJob
        {
            commandBuffer = barrier.CreateCommandBuffer().ToConcurrent(),
            entityArray = agentGroup.Entities
        };

        var handle =  addbufferJob.Schedule(agentGroup.Length, SimulationConstants.instance.BatchSize, inputDeps);

        handle.Complete();

        this.Enabled = false;
        return handle;

    }

}


[UpdateBefore(typeof(DataGatheringSystem))]
public class InitializeInteractionBuffer : JobComponentSystem
{

    [RequireComponentTag(typeof(AgentElement))]
    public struct AgentGroup
    {
        public ComponentDataArray<BioCrowds.AgentData> agentData;

        [ReadOnly] public EntityArray Entities;
        [ReadOnly] public readonly int Length;

    }

    [Inject] AgentGroup agentGroup;




    public struct FillBufferJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public BufferFromEntity<AgentElement> AgentBuffer;
        [ReadOnly] public EntityArray entities;


        public void Execute(int index)
        {
            var buffer = AgentBuffer[entities[index]];
            for (int i = 0; i < MemoryBufferLength.length; i++)
            {
                buffer.Add(new InteractionData { AgentID = i, numFrames = 0 });
            }


        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // FRAMECOUNT IS UPDATED IN JOB INSTANCING
        var survival_job = new FillBufferJob
        {
            entities = agentGroup.Entities,
            AgentBuffer = GetBufferFromEntity<AgentElement>(false)
        };

        var handle = survival_job.Schedule(agentGroup.Length, SimulationConstants.instance.BatchSize, inputDeps);


        this.Enabled = false;
        return handle;
    }


}



[UpdateAfter(typeof(BioCrowds.CellTagSystem))]
public class DataGatheringSystem : JobComponentSystem
{

    [Inject] BioCrowds.CellTagSystem cellTagSystem;


    public NativeMultiHashMap<int, int> nearbyAgents;

    protected override void OnStartRunning()
    {
        nearbyAgents = new NativeMultiHashMap<int, int>(ControlVariables.instance.agentQuantity * ControlVariables.instance.agentQuantity, Allocator.Persistent);
    }


    [RequireComponentTag(typeof(AgentElement))]
    public struct AgentGroup
    {
        [ReadOnly] public ComponentDataArray<Position> AgentPos;
        [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> AgentData;
        public ComponentDataArray<BioCrowds.AgentGoal> AgentGoal;
        [ReadOnly] public EntityArray Entities;

        [ReadOnly] public readonly int Length;
    }
    [Inject] public AgentGroup agentGroup;


    struct CloseForAWhile : IJobParallelFor
    {
        [ReadOnly] public NativeMultiHashMap<int, int> nearbyAgents;
        [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;
        [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> AgentData;
        [ReadOnly] public ComponentDataArray<Position> AgentPos;
        public ComponentDataArray<BioCrowds.AgentGoal> AgentGoal;
        [NativeDisableParallelForRestriction] public BufferFromEntity<AgentElement> AgentBuffer;
        [ReadOnly] public EntityArray Entities;


        public void Execute(int index)
        {
            int myID = AgentData[index].ID;
            float3 myPos = AgentPos[index].Value, closerPos;
            NativeMultiHashMapIterator<int> iter;
            var buffer = AgentBuffer[Entities[index]];


            //int i = 0, j = 0;
            //float3 agentPos;
            int closerID;

            bool keepgoing = nearbyAgents.TryGetFirstValue(myID, out closerID, out iter);

            if (!keepgoing) return;

 
            while (nearbyAgents.TryGetNextValue(out closerID, ref iter))
            {
                buffer[closerID] = new InteractionData { numFrames = buffer[closerID].interaction.numFrames + 1, AgentID = buffer[closerID].interaction.AgentID };
                

            }




        }
    }

    public struct FindNearbyAgents : IJobParallelFor
    {

        [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;

        [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> AgentData;

        [ReadOnly] public ComponentDataArray<Position> AgentPos;

        [WriteOnly] public NativeMultiHashMap<int, int>.Concurrent nearbyAgents;



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


        CloseForAWhile closeForAWhileJob = new CloseForAWhile
        {

            AgentIDToPos = cellTagSystem.AgentIDToPos,
            AgentData = agentGroup.AgentData,
            AgentPos = agentGroup.AgentPos,
            nearbyAgents = nearbyAgents,
            AgentGoal = agentGroup.AgentGoal,
            AgentBuffer = GetBufferFromEntity<AgentElement>(false),
            Entities = agentGroup.Entities

        };

        JobHandle jobHandleCloseForAWhile = closeForAWhileJob.Schedule(agentGroup.Length, 1, jobHandle);

        jobHandleCloseForAWhile.Complete();





        //for (int i = 0; i < agentGroup.Length; i++)
        //{
        //    int key = agentGroup.AgentData[i].ID;
        //    int item;
        //    NativeMultiHashMapIterator<int> it;
        //    if (nearbyAgents.TryGetFirstValue(key, out item, out it))
        //    {
        //        Debug.Log(key + " --> " + item);
        //    }

        //}




        return jobHandle;

    }


}