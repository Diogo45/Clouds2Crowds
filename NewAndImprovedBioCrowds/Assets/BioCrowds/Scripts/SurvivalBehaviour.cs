using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;


namespace BioCrowds
{
    //int how_many_frames_to_store = 10;

    public static class BufferParameter
    {
        public const int length = 150;
    }

    [InternalBufferCapacity(BufferParameter.length)]
    public struct DotElement : IBufferElementData
    {
        public static implicit operator float(DotElement e) { return e.dot; }
        public static implicit operator DotElement(float e) { return new DotElement { dot = e }; }
        public float dot;
    }

    public struct SurvivalComponent : IComponentData
    {
        public float threshold;

        // 0 = calm, 1 = panicked
        public int survival_state;
    }
    [DisableAutoCreation]
    [UpdateAfter(typeof(FluidMovementOnAgent))]
    public class SurvivalInstinctSystem : JobComponentSystem
    {
        //[RequireComponentTag(typeof(DotElement))]
        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public ComponentDataArray<AgentStep> AgentStep;
            public ComponentDataArray<SurvivalComponent> SurvivalComponent;

            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public readonly int Length;

        }

        private int frameCount = 0;
        private int bufferLength = BufferParameter.length;

        [Inject] public AgentGroup agentGroup;

        public struct SurvivalInstinctUpdateJob : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public ComponentDataArray<AgentStep> AgentStep;
            public ComponentDataArray<SurvivalComponent> SurvivalComponent;
            [NativeDisableParallelForRestriction] public BufferFromEntity<DotElement> AgentDotBuffer;
            [ReadOnly] public EntityArray entities;
            [ReadOnly] public int buffer_index;


            public void Execute(int index)
            {
                var buffer = AgentDotBuffer[entities[index]];
                buffer[buffer_index] = math.dot(math.normalize(AgentGoal[index].SubGoal - Position[index].Value), math.normalize(AgentStep[index].delta));


                float[] dot_sort = buffer.Reinterpret<float>().ToNativeArray().ToArray();

                Array.Sort(dot_sort);
                float median = (buffer[75] + buffer[76]) / 2.0f;

                if (median < SurvivalComponent[index].threshold && SurvivalComponent[index].survival_state == 0)
                {
                    SurvivalComponent[index] = new SurvivalComponent { threshold = SurvivalComponent[index].threshold, survival_state = 1 };
                }

            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // FRAMECOUNT IS UPDATED IN JOB INSTANCING
            var survival_job = new SurvivalInstinctUpdateJob
            {
                Position = agentGroup.Position,
                AgentGoal = agentGroup.AgentGoal,
                AgentStep = agentGroup.AgentStep,
                SurvivalComponent = agentGroup.SurvivalComponent,
                buffer_index = frameCount++ % bufferLength,
                entities = agentGroup.Entities,
                AgentDotBuffer = GetBufferFromEntity<DotElement>(false)
            };

            var survival_update_handle = survival_job.Schedule(agentGroup.Length, SimulationConstants.instance.BatchSize, inputDeps);


            return survival_update_handle;
        }

    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(FluidBarrier))]
    [UpdateBefore(typeof(FluidMovementOnAgent))]
    public class FillBufferSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(DotElement))]
        public struct AgentGroup
        {
            public ComponentDataArray<SurvivalComponent> SurvivalComponent;

            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public readonly int Length;

        }

        [Inject] AgentGroup agentGroup;

        public struct FillBufferJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public BufferFromEntity<DotElement> AgentDotBuffer;
            [ReadOnly] public EntityArray entities;


            public void Execute(int index)
            {
                var buffer = AgentDotBuffer[entities[index]];
                for (int i = 0; i < BufferParameter.length; i++)
                {
                    buffer.Add(1.0f);
                }


            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // FRAMECOUNT IS UPDATED IN JOB INSTANCING
            var survival_job = new FillBufferJob
            {
                entities = agentGroup.Entities,
                AgentDotBuffer = GetBufferFromEntity<DotElement>(false)
            };

            var handle = survival_job.Schedule(agentGroup.Length, SimulationConstants.instance.BatchSize, inputDeps);
            this.Enabled = false;
            return handle;
        }



    }

}