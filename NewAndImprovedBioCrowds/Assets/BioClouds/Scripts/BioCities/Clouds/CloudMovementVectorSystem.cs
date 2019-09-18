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
    /// Computes each cloud's Movement Vector for each simulation step.
    /// </summary>
    [UpdateAfter(typeof(CloudCellTotalWeightSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    public class CloudMovementVectorSystem : JobComponentSystem
    {
        public struct CloudDataGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<CloudGoal> CloudGoal;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [WriteOnly] public ComponentDataArray<CloudMoveStep> CloudStep;
            [ReadOnly] public readonly int Length;
        }
        [Inject] CloudDataGroup m_CloudDataGroup;
        [Inject] CellMarkSystem m_CellMarkSystem;
        [Inject] CloudCellTotalWeightSystem m_cloudCellTotalWeightSystem;
        [Inject] CellIDMapSystem m_cellIdMap;

        [Inject] CloudRightPreferenceSystem m_CloudSplit;
        
        struct CalculateCloudMoveStep : IJobParallelFor
        {

            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<CloudGoal> CloudGoals;
            [ReadOnly] public ComponentDataArray<Position> CloudPositions;
            [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;
            [ReadOnly] public NativeHashMap<int, float> CloudTotalW;
            [ReadOnly] public NativeHashMap<int, float3> CellMap;
            [WriteOnly] public ComponentDataArray<CloudMoveStep> CloudStep;
            [ReadOnly] public NativeHashMap<int, int> ExtraWeightCell;
            [ReadOnly] public bool useSplit;

            public void Execute(int index)
            {
                float3 currentCellPosition;
                NativeMultiHashMapIterator<int> it;

                float3 moveStep = float3.zero;
                float3 direction = float3.zero;
                CloudTotalW.TryGetValue(CloudData[index].ID, out float totalW);

                bool keepgoing = CloudMarkersMap.TryGetFirstValue(CloudData[index].ID, out currentCellPosition, out it);

                if (!keepgoing)
                    return;

                float F = CloudCalculations.GetF(currentCellPosition, CloudPositions[index].Value, CloudGoals[index].SubGoal - CloudPositions[index].Value);
                var auxinWeight = CloudCalculations.PartialW(totalW, F) * CloudData[index].MaxSpeed * (currentCellPosition - CloudPositions[index].Value);
                direction += auxinWeight;

                if (useSplit)
                    if(ExtraWeightCell.TryGetValue(CloudData[index].ID, out int extraweightcell))
                        if (GridConverter.Position2CellID(currentCellPosition) == extraweightcell)
                            //TODO dynamic extra weight
                            direction += 5 * auxinWeight;



                while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
                {
                    F = CloudCalculations.GetF(currentCellPosition, CloudPositions[index].Value, CloudGoals[index].SubGoal - CloudPositions[index].Value);
                    direction += CloudCalculations.PartialW(totalW, F) * CloudData[index].MaxSpeed * (currentCellPosition - CloudPositions[index].Value);


                    if (useSplit)
                        if (ExtraWeightCell.TryGetValue(CloudData[index].ID, out int extraweightcell))
                            if (GridConverter.Position2CellID(currentCellPosition) == extraweightcell)
                                direction += auxinWeight;

                }


                float moduleM = math.length(direction);
                float s = (float)(moduleM * math.PI);

                float3 normalized_direction = math.normalize(direction);
                float3 normalized_goalvector = math.normalize(CloudGoals[index].SubGoal - CloudPositions[index].Value);

                if (s > CloudData[index].MaxSpeed)
                    s = CloudData[index].MaxSpeed;

                if (moduleM > 0.00001f)
                    moveStep = s * (normalized_direction);
                else
                    moveStep = float3.zero;

                CloudStep[index] = new CloudMoveStep() { Delta = moveStep };

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            var calculateMoveStepJob = new CalculateCloudMoveStep()
            {
                CloudData = m_CloudDataGroup.CloudData,
                CloudGoals = m_CloudDataGroup.CloudGoal,
                CloudPositions = m_CloudDataGroup.Position,
                CloudStep = m_CloudDataGroup.CloudStep,
                CloudTotalW = m_cloudCellTotalWeightSystem.CloudTotalCellWeight,
                CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
                CellMap = m_cellIdMap.cellId2Cellfloat3,
                ExtraWeightCell = m_CloudSplit.extraWeightCellId,
                useSplit = Parameters.Instance.EnableRightPreference

            };

            var calculateMoveStepDeps = calculateMoveStepJob.Schedule(m_CloudDataGroup.Length, 64, inputDeps);

            calculateMoveStepDeps.Complete();

            return calculateMoveStepDeps;
        }

    }

}
