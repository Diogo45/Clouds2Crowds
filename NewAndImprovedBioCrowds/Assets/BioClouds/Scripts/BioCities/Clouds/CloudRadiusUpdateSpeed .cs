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
    /// Recalculates a cloud's radius based on its immediate density.
    /// The radius update follows the equations described on the paper.
    /// </summary>
    [UpdateAfter(typeof(CellMarkSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    [UpdateAfter(typeof(CloudMovementVectorSystem))]
    public class CloudRadiusUpdate : JobComponentSystem
    {
        public struct CloudDataGroup
        {
            public ComponentDataArray<CloudData> CloudData;
            public ComponentDataArray<CloudMoveStep> CloudStep;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;
        }
        [Inject] CloudDataGroup m_CloudDataGroup;
        [Inject] CellMarkSystem m_CellMarkSystem;
     
        struct CorrectRadiusJob : IJobParallelFor
        {
            public ComponentDataArray<CloudData> CloudData;
            public ComponentDataArray<CloudMoveStep> CloudStep;
            [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;
            [ReadOnly] public float CellArea;
            public void Execute(int index)
            {
                float3 currentCellPosition;
                int cellCount = 0;
                NativeMultiHashMapIterator<int> it;

                bool keepgoing = CloudMarkersMap.TryGetFirstValue(CloudData[index].ID, out currentCellPosition, out it);

                if (!keepgoing)
                    return;
                cellCount++;

                while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
                    cellCount++;

                float totalArea = cellCount * CellArea;

                CloudData cData = CloudData[index];

                float delta = cData.AgentQuantity / totalArea;

                float beta = math.min((math.pow(delta,2f) / math.pow(cData.PreferredDensity,2f)), 2f);
                
                beta = beta - 1.0f;
                
                float maxChange = cData.MaxSpeed;
                float radiusChange = math.max(-maxChange, (math.min(maxChange, cData.RadiusChangeSpeed * (beta) * cData.Radius)));

                cData.Radius += radiusChange;


                CloudData[index] = cData;


            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            CorrectRadiusJob CalculateWJob = new CorrectRadiusJob()
            {
                CloudData = m_CloudDataGroup.CloudData,
                CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
                CloudStep = m_CloudDataGroup.CloudStep,
                CellArea = Parameters.Instance.CellArea
            };


            var calculateRDeps = CalculateWJob.Schedule(m_CloudDataGroup.Length, 64, inputDeps);

            calculateRDeps.Complete();

            return calculateRDeps;
        }

    }

}