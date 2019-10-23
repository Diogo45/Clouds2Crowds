using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;


public struct SpawnedAgentsCounter : IComponentData
{
    public int Quantity;
}


public struct AgentCloudID : ISharedComponentData
{
    public int CloudID;
}

[UpdateAfter(typeof(BioClouds.CloudHeatMap))]
public class Clouds2CrowdsSystem : JobComponentSystem
{
    private static bool _ChangedWindow;
    private static void ChangedWindow(float3 newPosition, float2 newSize)
    {
        _ChangedWindow = true;
    }



    public static bool CheckCreatePosition(float3 pos)
    {
        if(_ChangedWindow)
            return CheckDesiredPosition(pos);
        else
            return WindowManager.CheckCreateZone(pos);
    }

    public static bool CheckDesiredPosition(float3 pos)
    {
        return WindowManager.CheckCreateZone(pos) || WindowManager.CheckVisualZone(pos);
    }

    public NativeHashMap<int, int> CloudID2AgentInWindow;
    public NativeHashMap<int, int> DesiredCloudID2AgentInWindow;
    public NativeHashMap<int, BioCrowds.AgentSpawner.Parameters> parameterBuffer;

    public NativeMultiHashMap<int, int> SpawnedAgentsInFrame;

    public struct CloudDataGroup 
    {
        public ComponentDataArray<SpawnedAgentsCounter> SpawnedAgents;
        [ReadOnly] public ComponentDataArray<BioClouds.CloudData> CloudData;
        [ReadOnly] public ComponentDataArray<BioClouds.CloudGoal> CloudGoal;
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
    [Inject] BioClouds.CellMarkSystem m_CellMarkSystem;
    [Inject] BioClouds.CellIDMapSystem m_CellID2PosSystem;
    [Inject] AgentsDataGroup m_AgentDataGroup;
    [Inject] BioClouds.CloudHeatMap m_heatmap;


    struct DesiredCloudAgent2CrowdAgentJob : IJobParallelFor
    {
        [ReadOnly] public ComponentDataArray<BioClouds.CloudData> CloudData;

        [ReadOnly] public NativeHashMap<int, float3> cellid2pos;
        [ReadOnly] public NativeHashMap<int, float> cloudDensities;

        [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;
        
        [WriteOnly] public NativeHashMap<int, int>.Concurrent desiredQuantity;

        public void Execute(int index)
        {
            BioClouds.CloudData currentCloudData = CloudData[index];

            float3 currentCellPosition;
            int desiredCellQnt = 0;
            int cellCount = 0;
            NativeMultiHashMapIterator<int> it;
            
            bool keepgoing = CloudMarkersMap.TryGetFirstValue(currentCloudData.ID, out currentCellPosition, out it);
            if (!keepgoing)
                return;

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
                int agentQuantity = (int)(desiredCellQnt * cloudDensity * BioClouds.Parameters.Instance.CellArea);
                desiredQuantity.TryAdd(currentCloudData.ID, agentQuantity);
            }

        }
    }

    struct AddDifferencePerCloudJob : IJobParallelFor
    {
        [ReadOnly] public ComponentDataArray<BioClouds.CloudData> CloudData;
        [ReadOnly] public ComponentDataArray<BioClouds.CloudGoal> CloudGoal;
        [ReadOnly] public ComponentDataArray<SpawnedAgentsCounter> Counter;

        [ReadOnly] public NativeHashMap<int, int> CloudID2AgentInWindow;
        [ReadOnly] public NativeHashMap<int, int> DesiredCloudID2AgentInWindow;
        [WriteOnly] public NativeMultiHashMap<int, int>.Concurrent AddedAgentsPerCloud;

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


            //fetch desired cloud agents in window
            int desiredAgentsInWindow;
            if (!DesiredCloudID2AgentInWindow.TryGetValue(currentCloudID, out desiredAgentsInWindow))
                return;


            //create um menos o outro
            
            int agentsToCreate = (int)math.max(desiredAgentsInWindow - agentsInWindow, 0f);


            List<float3> positionList = new List<float3>();


            NativeMultiHashMapIterator<int> it;
            float3 currentCellPosition;
            bool keepgoing = CloudMarkersMap.TryGetFirstValue(currentCloudID, out currentCellPosition, out it);

            if (!keepgoing)
                return;



            if (CheckCreatePosition(currentCellPosition))
            {

                positionList.Add(currentCellPosition);

            }

            while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
            {
                
                if (CheckCreatePosition(currentCellPosition))
                {
                    positionList.Add(currentCellPosition);

                }
            }

            agentsToCreate = math.min(CloudData[index].AgentQuantity - Counter[index].Quantity, agentsToCreate);

            int spawnPerCell = (int)math.max(math.ceil((float)agentsToCreate / positionList.Count), 0f);

            int spawned = 0;

            //TODO: Fix postionList count == 0
            if (positionList.Count <= 0) return;

            //Random Distribution
            System.Random r = new System.Random(System.DateTime.UtcNow.Millisecond);
            while (agentsToCreate - spawned > 0)
            {
                float3 position = positionList[r.Next(positionList.Count - 1)];
                //create agent
                BioCrowds.AgentSpawner.Parameters par = new BioCrowds.AgentSpawner.Parameters
                {
                    cloud = currentCloudID,
                    goal = CloudGoal[index].SubGoal,
                    maxSpeed = CloudData[index].MaxSpeed,
                    qtdAgents = math.min(agentsToCreate - spawned, spawnPerCell),
                    spawnDimensions = new float2 { x = 2f, y = 2f },
                    spawnOrigin = position
                };


                buffer.TryAdd(GridConverter.Position2CellID(position), par);
                AddedAgentsPerCloud.Add(currentCloudID, par.qtdAgents);
                spawned += par.qtdAgents;
            }

          
        }
    }

    struct CloudAgentAccumulator : IJobParallelFor
    {

        [ReadOnly] public ComponentDataArray<BioClouds.CloudData> CloudData;
        [ReadOnly] public NativeMultiHashMap<int, int> AddedAgentsInFramePerCloud;
        public ComponentDataArray<SpawnedAgentsCounter> Counter;

        public void Execute(int index)
        {
            int count = Counter[index].Quantity;

            NativeMultiHashMapIterator<int> it;

            int quantity;
            if (AddedAgentsInFramePerCloud.TryGetFirstValue(CloudData[index].ID, out quantity, out it))
            {
                count += quantity;

                while (AddedAgentsInFramePerCloud.TryGetNextValue(out quantity, ref it))
                {
                    count += quantity;
                }
            }
            Counter[index] = new SpawnedAgentsCounter { Quantity = count };
        }

    }

    struct ResetCloudAccumulator : IJobParallelFor
    {

        [ReadOnly] public ComponentDataArray<BioClouds.CloudData> CloudData;
        [ReadOnly] public NativeHashMap<int, int> AgentsPerCloud;
        public ComponentDataArray<SpawnedAgentsCounter> Counter;

        public void Execute(int index)
        {
            int count = Counter[index].Quantity;
            

            int quantity;
            if (AgentsPerCloud.TryGetValue(CloudData[index].ID, out quantity))
            {
                count = quantity;
            }
            else
            {
                count = 0;
            }
            Counter[index] = new SpawnedAgentsCounter { Quantity = count };
        }

    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        CloudID2AgentInWindow = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);
        DesiredCloudID2AgentInWindow = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);
        parameterBuffer = new NativeHashMap<int, BioCrowds.AgentSpawner.Parameters>(80000, Allocator.Persistent); //TODO make dynamic

        SpawnedAgentsInFrame =new NativeMultiHashMap<int, int>(80000, Allocator.Persistent);
        WindowManager.MovedWindow += ChangedWindow;

    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
        CloudID2AgentInWindow.Dispose();
        DesiredCloudID2AgentInWindow.Dispose();
        parameterBuffer.Dispose();
        SpawnedAgentsInFrame.Dispose();
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
        SpawnedAgentsInFrame.Clear();

        var cg = ComponentGroups[m_AgentDataGroup.GroupIndex];


        for(int i = 0; i < m_CloudDataGroup.Length; i++)
        {
            AgentCloudID newCloudID = new AgentCloudID { CloudID = i };
            cg.SetFilter<AgentCloudID>(newCloudID);


            int quantity = cg.CalculateLength();

            CloudID2AgentInWindow.TryAdd(i, quantity);
        }


        if (_ChangedWindow)
        {
            var resetAcummulators = new ResetCloudAccumulator
            {
                AgentsPerCloud = CloudID2AgentInWindow,
                CloudData = m_CloudDataGroup.CloudData,
                Counter = m_CloudDataGroup.SpawnedAgents
            };

            var resetJob = resetAcummulators.Schedule(m_CloudDataGroup.Length, 1, inputDeps);
            resetJob.Complete();
        }


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
            DesiredCloudID2AgentInWindow = DesiredCloudID2AgentInWindow,
            AddedAgentsPerCloud = SpawnedAgentsInFrame.ToConcurrent(),
            Counter = m_CloudDataGroup.SpawnedAgents
        };

        var diffJobHandle = differenceJob.Schedule(m_CloudDataGroup.Length, 1, inputDeps);

        diffJobHandle.Complete();


        CloudAgentAccumulator accumulatorJob = new CloudAgentAccumulator
        {
            AddedAgentsInFramePerCloud = SpawnedAgentsInFrame,
            Counter = m_CloudDataGroup.SpawnedAgents,
            CloudData = m_CloudDataGroup.CloudData
        };

        var accumJobHandle = accumulatorJob.Schedule(m_CloudDataGroup.Length, 1, diffJobHandle);
        accumJobHandle.Complete();


        _ChangedWindow = false;
        return accumJobHandle;
    }

}

    