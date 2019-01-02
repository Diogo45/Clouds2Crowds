using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

[UpdateAfter(typeof(BioCities.CloudHeatMap))]
public class Clouds2CrowdsSystem : JobComponentSystem
{

    public static bool CheckPosition(float3 pos)
    {
        return WindowManager.CheckVisualZone(pos);
    }

    public NativeHashMap<int, int> CloudID2AgentInWindow;
    public NativeHashMap<int, int> DesiredCloudID2AgentInWindow;

    public struct CloudDataGroup
    {
        [ReadOnly] public ComponentDataArray<BioCities.CloudData> CloudData;
        [ReadOnly] public ComponentDataArray<BioCities.CloudGoal> CloudGoal;
        [ReadOnly] public ComponentDataArray<Position> Position;
        [ReadOnly] public readonly int Length;
    }

    public struct AgentsDataGroup
    {
        [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> CloudData;
    }

    [Inject] CloudDataGroup m_CloudDataGroup;
    [Inject] BioCities.CellMarkSystem m_CellMarkSystem;
    [Inject] BioCities.CellIDMapSystem m_CellID2PosSystem;
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
            cellCount++;
            if(CheckPosition(currentCellPosition))
            {
                desiredCellQnt++;
            }

            while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
            {
                cellCount++;
                if (CheckPosition(currentCellPosition))
                {
                    desiredCellQnt++;
                }
            }
            float cloudDensity;

            if (cloudDensities.TryGetValue(currentCloudData.ID, out cloudDensity))
            {

                int agentQuantity = (int)(desiredCellQnt * cloudDensity);
                desiredQuantity.TryAdd(currentCloudData.ID, agentQuantity);
            }

        }
    }

    struct CurrentCloudAgent2CrowdAgentJob : IJobParallelFor
    {
        [ReadOnly] public ComponentDataArray<BioCities.CloudData> CloudData;

        [ReadOnly] public NativeHashMap<int, float3> cellid2pos;
        [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;

        [ReadOnly] public ComponentDataArray<BioCrowds.AgentData> AgentData;


        [WriteOnly] public NativeHashMap<int, int> desiredQuantity;

        public void Execute(int index)
        {
            BioCities.CloudData currentCloudData = CloudData[index];


            float3 currentCellPosition;
            int cellCount = 0;
            NativeMultiHashMapIterator<int> it;
            float3 posSum = float3.zero;

            bool keepgoing = CloudMarkersMap.TryGetFirstValue(currentCloudData.ID, out currentCellPosition, out it);

            if (!keepgoing)
                return;

            cellCount++;



            while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
            {
                cellCount++;

            }

        }
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        CloudID2AgentInWindow = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);
        DesiredCloudID2AgentInWindow = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
        CloudID2AgentInWindow.Dispose();
        DesiredCloudID2AgentInWindow.Dispose();
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
        

        //for (int i = 0; i < m_CloudDataGroup.Length; i++)
        //{
        //Debug.DrawLine(m_CloudDataGroup.Position[i].Value, sums[i] + m_CloudDataGroup.Position[i].Value, Color.yellow);
        //Debug.DrawLine(m_CloudDataGroup.Position[i].Value, dessums[i] + m_CloudDataGroup.Position[i].Value, Color.green);
        //    if (dotVector[i] < 0f)
        //       Debug.DrawLine(m_CloudDataGroup.Position[i].Value, sums[i] - dessums[i]  + m_CloudDataGroup.Position[i].Value, Color.magenta);
        //}

        return inputDeps;
    }

}

    