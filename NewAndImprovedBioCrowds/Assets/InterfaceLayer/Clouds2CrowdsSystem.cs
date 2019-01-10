using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;


public struct AgentCloudID : ISharedComponentData
{
    public int CloudID;
}

[UpdateAfter(typeof(BioCities.CloudHeatMap))]
public class Clouds2CrowdsSystem : JobComponentSystem
{
    public static bool CheckCreatePosition(float3 pos)
    {
        return WindowManager.CheckCreateZone(pos);
    }

    public static bool CheckDesiredPosition(float3 pos)
    {
        return WindowManager.CheckVisualZone(pos);
    }

    public NativeHashMap<int, int> CloudID2AgentInWindow;
    public NativeHashMap<int, int> DesiredCloudID2AgentInWindow;
    public NativeHashMap<int, BioCrowds.AgentSpawner.Parameters> parameterBuffer;
    public NativeHashMap<int, int> SpawnedAgentsInFrame;
    public NativeHashMap<int, int> TotalSpawnedAgentsPerCloud;

    public struct CloudDataGroup 
    {
        [ReadOnly] public ComponentDataArray<BioCities.CloudData> CloudData;
        [ReadOnly] public ComponentDataArray<BioCities.CloudGoal> CloudGoal;
        [ReadOnly] public ComponentDataArray<Position> Position;
        [ReadOnly] public readonly int Length;
    }

    public struct AgentsDataGroup
    {
        [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> AgentData;
        [ReadOnly] public ComponentDataArray<Position> Position;
        [ReadOnly] public SharedComponentDataArray<AgentCloudID> AgentCloudID;
        public readonly int GroupIndex;
    }

    [Inject] CloudDataGroup m_CloudDataGroup;
    [Inject] BioCities.CellMarkSystem m_CellMarkSystem;
    [Inject] BioCities.CellIDMapSystem m_CellID2PosSystem;
    [Inject] AgentsDataGroup m_AgentDataGroup;
    [Inject] BioCities.CloudHeatMap m_heatmap;


    struct DesiredCloudAgent2CrowdAgentJob : IJobParallelFor
    {
        [ReadOnly] public ComponentDataArray<BioCities.CloudData> CloudData;

        [ReadOnly] public NativeHashMap<int, float3> cellid2pos;
        [ReadOnly] public NativeHashMap<int, float> cloudDensities;

        [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;
        
        [WriteOnly] public NativeHashMap<int, int>.Concurrent desiredQuantity;

        public void Execute(int index)
        {
            BioCities.CloudData currentCloudData = CloudData[index];

            float3 currentCellPosition;
            int desiredCellQnt = 0;
            int cellCount = 0;
            NativeMultiHashMapIterator<int> it;
            
            bool keepgoing = CloudMarkersMap.TryGetFirstValue(currentCloudData.ID, out currentCellPosition, out it);
            if (!keepgoing)
                return;

            //Debug.Log("DESIRED1");


            cellCount++;
            if(CheckDesiredPosition(currentCellPosition))
            {
                desiredCellQnt++;
            }

            while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
            {
                cellCount++;
                if (CheckDesiredPosition(currentCellPosition))
                {
                    desiredCellQnt++;
                }
            }
            float cloudDensity;

            if (cloudDensities.TryGetValue(currentCloudData.ID, out cloudDensity))
            {
                //Debug.Log("DESIRED2");

                int agentQuantity = (int)(desiredCellQnt * cloudDensity * BioCities.Parameters.Instance.CellArea);
                desiredQuantity.TryAdd(currentCloudData.ID, agentQuantity);
            }

        }
    }

    //struct CurrentInAreaJob : IJobParallelFor
    //{
    //    [ReadOnly] public ComponentDataArray<BioCities.CloudData> CloudData;
    //    [ReadOnly] public ComponentDataArray<Position> Position;

    //    [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;
    //    [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> AgentData;

    //    [WriteOnly] public NativeHashMap<int, int>.Concurrent CloudID2AgentInWindow;

    //    public void Execute(int index)
    //    {
    //        BioCities.CloudData currentCloudData = CloudData[index];
    //        Position currentCloudPosition = Position[index];


    //        float3 currentCellPosition;
    //        int cellCount = 0;
    //        NativeMultiHashMapIterator<int> it;
    //        float3 posSum = float3.zero;

    //        bool keepgoing = CloudMarkersMap.TryGetFirstValue(currentCloudData.ID, out currentCellPosition, out it);

    //        if (!keepgoing)
    //            return;

    //        cellCount++;



    //        while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
    //        {
    //            cellCount++;

    //        }

    //    }
    //}

    struct AddDifferencePerCloudJob : IJobParallelFor
    {
        [ReadOnly] public ComponentDataArray<BioCities.CloudData> CloudData;
        [ReadOnly] public ComponentDataArray<BioCities.CloudGoal> CloudGoal;

        [ReadOnly] public NativeHashMap<int, int> CloudID2AgentInWindow;
        [ReadOnly] public NativeHashMap<int, int> DesiredCloudID2AgentInWindow;
        [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;

        [WriteOnly] public NativeHashMap<int, BioCrowds.AgentSpawner.Parameters>.Concurrent buffer;

        public void Execute(int index)
        {
            //fetch cloud id
            int currentCloudID = CloudData[index].ID;

            //fetch cloud agents in window
            int agentsInWindow;
            if (!CloudID2AgentInWindow.TryGetValue(currentCloudID, out agentsInWindow))
                return;

           // Debug.Log("PASS1");


            //fetch desired cloud agents in window
            int desiredAgentsInWindow;
            if (!DesiredCloudID2AgentInWindow.TryGetValue(currentCloudID, out desiredAgentsInWindow))
                return;

            //Debug.Log("PASS2");

            //create um menos o outro
            
            int agentsToCreate = (int)math.max(desiredAgentsInWindow - agentsInWindow, 0f);

            if (agentsToCreate < 0)
                Debug.Log("DEU MEME GURIZADA");

            List<float3> positionList = new List<float3>();

            //TODO DESPAWN FORA DA JANELA LEMBRAAAAAR

            NativeMultiHashMapIterator<int> it;
            float3 currentCellPosition;
            bool keepgoing = CloudMarkersMap.TryGetFirstValue(currentCloudID, out currentCellPosition, out it);

            if (!keepgoing)
                return;

            //Debug.Log("PASSTOT");

            if (CheckCreatePosition(currentCellPosition))
            {
                //Debug.Log(currentCellPosition);
                positionList.Add(currentCellPosition);

            }

            while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
            {
                
                if (CheckCreatePosition(currentCellPosition))
                {
                    positionList.Add(currentCellPosition);
                    //Debug.Log(currentCellPosition);
                }
            }

            foreach(float3 position in positionList)
            {
                //create agent
                BioCrowds.AgentSpawner.Parameters par = new BioCrowds.AgentSpawner.Parameters
                {
                    cloud = currentCloudID,
                    goal = CloudGoal[index].SubGoal,
                    maxSpeed = CloudData[index].MaxSpeed,
                    qtdAgents = (int)math.max(agentsToCreate / positionList.Count, 0f),
                    spawnDimensions = new float2 { x = 2f, y = 2f },
                    spawnOrigin = position
                };
                //Debug.Log("CREATE AGENT " + currentCloudID);

                buffer.TryAdd(GridConverter.Position2CellID(position), par);
            }

        }
    }

    struct CloudAgentAccumulator : IJobParallelFor
    {

        [ReadOnly] public NativeHashMap<int, int> AddedAgentsInFrame;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, int> TotalAgents;
        

        public void Execute(int index)
        {
            
        }

    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        CloudID2AgentInWindow = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);
        DesiredCloudID2AgentInWindow = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);
        parameterBuffer = new NativeHashMap<int, BioCrowds.AgentSpawner.Parameters>(80000, Allocator.Persistent); //TODO make dynamic

    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
        CloudID2AgentInWindow.Dispose();
        DesiredCloudID2AgentInWindow.Dispose();
        parameterBuffer.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (DesiredCloudID2AgentInWindow.Capacity != m_CloudDataGroup.Length * 2)
        {
            DesiredCloudID2AgentInWindow.Dispose();
            DesiredCloudID2AgentInWindow = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);

            CloudID2AgentInWindow.Dispose();
            CloudID2AgentInWindow = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);
        }
        else
        {
            DesiredCloudID2AgentInWindow.Clear();
            CloudID2AgentInWindow.Clear();
        }

        parameterBuffer.Clear();

        var cg = ComponentGroups[m_AgentDataGroup.GroupIndex];

        //JobHandle[] jobs =  new JobHandle[m_CloudDataGroup.Length];
        for(int i = 0; i < m_CloudDataGroup.Length; i++)
        {
            AgentCloudID newCloudID = new AgentCloudID { CloudID = i };
            cg.SetFilter<AgentCloudID>(newCloudID);

            //CurrentInAreaJob newAreajob = new CurrentInAreaJob
            //{
            //    AgentData = cg.GetComponentDataArray<BioCrowds.AgentData>(),
            //    CloudData = m_CloudDataGroup.CloudData,
            //    CloudID2AgentInWindow = CloudID2AgentInWindow.ToConcurrent(),
            //    CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
            //    Position = cg.GetComponentDataArray<Position>()
            //};

            //jobs[i] = newAreajob.Schedule(m_CloudDataGroup.Length, 1, inputDeps);

            CloudID2AgentInWindow.TryAdd(i, cg.CalculateLength());
        }

        //for (int i = 0; i < m_CloudDataGroup.Length; i++)
        //{
        //    jobs[i].Complete();
        //}


        var desiredJob = new DesiredCloudAgent2CrowdAgentJob
        {
            cellid2pos = m_CellID2PosSystem.cellId2Cellfloat3,
            cloudDensities = m_heatmap.cloudDensities,
            CloudData = m_CloudDataGroup.CloudData,
            CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
            desiredQuantity = DesiredCloudID2AgentInWindow.ToConcurrent()
        };

        var desiredJobHandle = desiredJob.Schedule(m_CloudDataGroup.Length, 1, inputDeps);

        desiredJobHandle.Complete();

        AddDifferencePerCloudJob differenceJob = new AddDifferencePerCloudJob
        {
            buffer = parameterBuffer.ToConcurrent(),
            CloudData = m_CloudDataGroup.CloudData,
            CloudGoal = m_CloudDataGroup.CloudGoal,
            CloudID2AgentInWindow = CloudID2AgentInWindow,
            CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
            DesiredCloudID2AgentInWindow = DesiredCloudID2AgentInWindow
        };

        var diffJobHandle = differenceJob.Schedule(m_CloudDataGroup.Length, 1, inputDeps);

        diffJobHandle.Complete();

        //Debug.Log("L1 " + parameterBuffer.Length);


        return diffJobHandle;
    }

}

    