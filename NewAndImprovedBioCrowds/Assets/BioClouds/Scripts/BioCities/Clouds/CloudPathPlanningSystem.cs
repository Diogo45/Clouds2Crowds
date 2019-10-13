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

    [DisableAutoCreation]
    [UpdateAfter(typeof(CloudMoveSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    public class CloudPathPlanningSystem : JobComponentSystem
    {
        public struct CloudDataGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            public ComponentDataArray<CloudGoal> CloudGoal;
            [ReadOnly] public ComponentDataArray<CloudMoveStep> CloudStep;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;
        }
        [Inject] CloudDataGroup m_CloudDataGroup;

        public static int stoppedFrames = 30;
        public static float minimumMovement = 0.01f;

        public static float closenessToWayPoint = 0.1f;

        
        struct UpdateGoalJob : IJobParallelFor
        {

            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            public ComponentDataArray<CloudGoal> CloudGoals;
            [ReadOnly] public ComponentDataArray<CloudMoveStep> CloudStep;
            [ReadOnly] public ComponentDataArray<Position> CloudPositions;

            public void Execute(int index)
            {
                float3 cloudPos = CloudPositions[index].Value;
                CloudGoal cg = CloudGoals[index];
                int end_id = cg.EndObjectiveID;
                float movementMaginitude = math.lengthsq(CloudStep[index].Delta);
                int moveless = 0;
                if(movementMaginitude < minimumMovement)
                {
                    moveless = cg.MovelessFrames + 1;
                }

                cg = new CloudGoal
                {
                    CurrentObjectiveID = cg.CurrentObjectiveID,
                    EndGoal = cg.EndGoal,
                    EndObjectiveID = cg.EndObjectiveID,
                    MovelessFrames = moveless,
                    SubGoal = cg.SubGoal
                };

                if (cg.MovelessFrames > stoppedFrames || math.distance(cloudPos, new float3(cg.SubGoal.x, cg.SubGoal.y, cg.SubGoal.z)) < closenessToWayPoint)
                {

                    PathManager.WayPoint next = PathManager.GetNext(CloudPositions[index].Value, cg.EndObjectiveID);
                    Debug.Log("new path, old current = " + cg.CurrentObjectiveID + " objective" + cg.EndObjectiveID + " new next" + next.ID);
                    cg.CurrentObjectiveID = next.ID;
                    cg.SubGoal = new float3(next.x, next.y, 0);
                    cg.MovelessFrames = 0;

                }

                Debug.Log("new end objective " + end_id + " " + cg.EndObjectiveID);
                CloudGoals[index] = cg;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            var updateGoal = new UpdateGoalJob()
            {
                CloudData = m_CloudDataGroup.CloudData,
                CloudGoals = m_CloudDataGroup.CloudGoal,
                CloudStep = m_CloudDataGroup.CloudStep,
                CloudPositions = m_CloudDataGroup.Position
            };

            var updateGoalDeps = updateGoal.Schedule(m_CloudDataGroup.Length, 64, inputDeps);

            return updateGoalDeps;
        }

    }

}

