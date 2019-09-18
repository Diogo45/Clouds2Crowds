using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;


namespace BioClouds {



    /// <summary>
    /// Computes a mapping of Cloud ID to total weight for captured markers.
    /// </summary>
    [UpdateAfter(typeof(CellMarkSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    public class CloudCellTotalWeightSystem : JobComponentSystem
    {
        //data structure sizes
        int lastsize_CloudTotalAuxinWeight;
        

        public struct CloudDataGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<CloudGoal> CloudGoal;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;
        }
        [Inject] CloudDataGroup m_CloudDataGroup;
        [Inject] CellMarkSystem m_CellMarkSystem;

        /// <summary>
        /// Mapping of Cloud ID to total captured marker cell weight.
        /// </summary>
        public NativeHashMap<int, float> CloudTotalCellWeight;

        struct CalculateTotalMarkerWeight : IJobParallelFor
        {
            [WriteOnly] public NativeHashMap<int, float>.Concurrent cloudTotalAuxinWeight;

            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<CloudGoal> CloudGoals;
            [ReadOnly] public ComponentDataArray<Position> CloudPositions;
            [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;


            public void Execute(int index)
            {
                float3 currentCellPosition;
                NativeMultiHashMapIterator<int> it;

                float totalW = 0f;

                bool keepgoing = CloudMarkersMap.TryGetFirstValue(CloudData[index].ID, out currentCellPosition, out it);

                if (!keepgoing)
                    return;

                totalW += CloudCalculations.GetF(currentCellPosition, CloudPositions[index].Value, (CloudGoals[index].SubGoal - CloudPositions[index].Value));

                while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
                    totalW += CloudCalculations.GetF(currentCellPosition, CloudPositions[index].Value, (CloudGoals[index].SubGoal - CloudPositions[index].Value));

                cloudTotalAuxinWeight.TryAdd(CloudData[index].ID, totalW);

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            int size = m_CloudDataGroup.Length * 2;
            if (lastsize_CloudTotalAuxinWeight != size)
            {
                CloudTotalCellWeight.Dispose();
                CloudTotalCellWeight = new NativeHashMap<int, float>(size, Allocator.Persistent);
            }
            else
                CloudTotalCellWeight.Clear();
            lastsize_CloudTotalAuxinWeight = size;

            CalculateTotalMarkerWeight CalculateWJob = new CalculateTotalMarkerWeight()
            {
                cloudTotalAuxinWeight = CloudTotalCellWeight.ToConcurrent(),
                CloudData = m_CloudDataGroup.CloudData,
                CloudGoals = m_CloudDataGroup.CloudGoal,
                CloudPositions = m_CloudDataGroup.Position,
                CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap
            };


            var calculateWDeps = CalculateWJob.Schedule(m_CloudDataGroup.Length, 64, inputDeps);

            calculateWDeps.Complete();

            return calculateWDeps;
        }

        protected override void OnStartRunning()
        {
            CloudTotalCellWeight = new NativeHashMap<int, float>(0, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            if (CloudTotalCellWeight.IsCreated)
                CloudTotalCellWeight.Dispose();
        }
    }

}