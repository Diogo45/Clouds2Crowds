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
    /// Moves each cloud according to the CloudMoveStep previously computed.
    /// </summary>
    [UpdateAfter(typeof(CloudMovementVectorSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    public class CloudMoveSystem : JobComponentSystem
    {

        public struct CellsGroup
        {
            [WriteOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<CloudMoveStep> CloudStep;
            [ReadOnly] public readonly int Length;
        }
        [Inject] CellsGroup m_CellGroup;

        struct MoveCloudsJob : IJobParallelFor
        {
            public ComponentDataArray<Position> Positions;
            [ReadOnly] public ComponentDataArray<CloudMoveStep> Deltas;

            public void Execute(int index)
            {
                float3 old = Positions[index].Value;

                Positions[index] = new Position { Value = old + Deltas[index].Delta };
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            MoveCloudsJob moveJob = new MoveCloudsJob()
            {
                Positions = m_CellGroup.Position,
                Deltas = m_CellGroup.CloudStep
            };

            var deps = moveJob.Schedule(m_CellGroup.Length, 64, inputDeps);

            deps.Complete();

            return deps;
        }

    }
    
}