using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace BioClouds
{
    /// <summary>
    /// Enables clouds to follow a right preference while being squashed.
    /// This system attributes increased weight to a marker on the right side of movement of a cloud.
    /// </summary>
    [UpdateAfter(typeof(CellMarkSystem))]
    [UpdateBefore(typeof(CloudMovementVectorSystem))]
    public class CloudRightPreferenceSystem : JobComponentSystem
    {
        public NativeArray<float3> sums;
        public NativeArray<float3> dessums;
        public NativeArray<float> dotVector;
        public NativeArray<float3> crossVector;
        public NativeHashMap<int, int> extraWeightCellId;

        public struct CloudDataGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<CloudGoal> CloudGoal;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;
        }
        [Inject] CloudDataGroup m_CloudDataGroup;
        [Inject] CellMarkSystem m_CellMarkSystem;
        [Inject] CellIDMapSystem m_CellID2PosSystem;

        struct CloudSplitSJob : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<CloudGoal> CloudGoal;
            [ReadOnly] public ComponentDataArray<Position> Position;

            [ReadOnly] public NativeHashMap<int, float3> cellid2pos;
            [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;

            [WriteOnly] public NativeArray<float3> sumVec;
            [WriteOnly] public NativeArray<float3> dessumVec;
            [WriteOnly] public NativeArray<float3> crossVec;
            [WriteOnly] public NativeArray<float> dotVec;
            [WriteOnly] public NativeHashMap<int, int>.Concurrent extraWeightCellId;

            public void Execute(int index)
            {
                CloudData currentCloudData = CloudData[index];
                CloudGoal currentCloudGoal = CloudGoal[index];
                Position currentCloudPosition = Position[index];
                

                int[] desiredCells = GridConverter.RadiusInGrid(currentCloudPosition.Value, currentCloudData.Radius);
                float3 desiredSum = float3.zero;

                for(int i =0; i < desiredCells.Length; i++)
                {
                    float3 partial;
                    if (cellid2pos.TryGetValue(desiredCells[i], out partial)){ 
                       var s = math.length(partial - currentCloudPosition.Value);
                        if(s <= currentCloudData.Radius)
                        {
                            desiredSum += (math.normalize(partial - currentCloudPosition.Value)) * (currentCloudData.Radius - s);
                        }
                    
                    }

                }

                float3 currentCellPosition;
                int cellCount = 0;
                NativeMultiHashMapIterator<int> it;
                float3 posSum = float3.zero;

                bool keepgoing = CloudMarkersMap.TryGetFirstValue(currentCloudData.ID, out currentCellPosition, out it);

                if (!keepgoing)
                    return;
                
                cellCount++;
                var t = math.length(currentCellPosition - currentCloudPosition.Value);
                posSum += (math.normalize(currentCellPosition - currentCloudPosition.Value)) * (currentCloudData.Radius - t);
                bool right_desempate = false;

                if (!right_desempate && math.cross(currentCloudPosition.Value, currentCellPosition - currentCloudPosition.Value).z < 0)
                {
                    extraWeightCellId.TryAdd(currentCloudData.ID, GridConverter.Position2CellID(currentCellPosition));
                    right_desempate = true;
                }


                while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
                {
                    cellCount++;

                    t = math.length(currentCellPosition - currentCloudPosition.Value);
                    if (t <= currentCloudData.Radius)
                    {
                    posSum += (math.normalize(currentCellPosition - currentCloudPosition.Value)) * (currentCloudData.Radius - t);
                    }

                    if (!right_desempate && math.cross(currentCloudPosition.Value, currentCellPosition - currentCloudPosition.Value).z < 0)
                    {
                        extraWeightCellId.TryAdd(currentCloudData.ID, GridConverter.Position2CellID(currentCellPosition));
                        right_desempate = true;
                    }

                }

                sumVec[index] = posSum;
                dessumVec[index] = desiredSum;
                dotVec[index] = math.dot((CloudGoal[index].SubGoal - currentCloudPosition.Value), posSum - desiredSum);
            }
        }

        protected override void OnStartRunning()
        {
            sums = new NativeArray<float3>(m_CloudDataGroup.Length, Allocator.Persistent);
            dessums = new NativeArray<float3>(m_CloudDataGroup.Length, Allocator.Persistent);
            dotVector = new NativeArray<float>(m_CloudDataGroup.Length, Allocator.Persistent);
            crossVector = new NativeArray<float3>(m_CloudDataGroup.Length, Allocator.Persistent);
            extraWeightCellId = new NativeHashMap<int, int>(m_CloudDataGroup.Length*2, Allocator.Persistent);
        }
        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if(sums.IsCreated)
                sums.Dispose();

            if(dessums.IsCreated)
                dessums.Dispose();

            if(dotVector.IsCreated)
                dotVector.Dispose();

            if(crossVector.IsCreated)
                crossVector.Dispose();

            if(extraWeightCellId.IsCreated)
                extraWeightCellId.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (sums.Length != m_CloudDataGroup.Length)
            {
                sums.Dispose();
                sums = new NativeArray<float3>(m_CloudDataGroup.Length, Allocator.Persistent);
                dessums.Dispose();
                dessums = new NativeArray<float3>(m_CloudDataGroup.Length, Allocator.Persistent);
                dotVector.Dispose();
                dotVector = new NativeArray<float>(m_CloudDataGroup.Length, Allocator.Persistent);
                crossVector.Dispose();
                crossVector = new NativeArray<float3>(m_CloudDataGroup.Length, Allocator.Persistent);
                extraWeightCellId.Dispose();
                extraWeightCellId = new NativeHashMap<int, int>(m_CloudDataGroup.Length * 2, Allocator.Persistent);
            }
            else
                extraWeightCellId.Clear();


            CloudSplitSJob CalculateWJob = new CloudSplitSJob()
            {
                CloudData = m_CloudDataGroup.CloudData,
                CloudGoal = m_CloudDataGroup.CloudGoal,
                Position = m_CloudDataGroup.Position,
                CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
                cellid2pos = m_CellID2PosSystem.cellId2Cellfloat3,
                sumVec = sums,
                dessumVec = dessums,
                dotVec = dotVector,
                crossVec = crossVector,
                extraWeightCellId = extraWeightCellId.ToConcurrent()
            };


            var calculateRDeps = CalculateWJob.Schedule(m_CloudDataGroup.Length, 64, inputDeps);

            calculateRDeps.Complete();

            
            return calculateRDeps;
        }

    }


}