using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine;
using System;

namespace BioCrowds
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(AgentMovementSystem)),
     UpdateAfter(typeof(AgentMovementVectors)),
      UpdateInGroup(typeof(MovementVectorsSystemGroup))]
    public class AgentMovementTimeMachine : JobComponentSystem
    {

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [WriteOnly] public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public readonly int Length;
        }


        [Inject] AgentGroup agentGroup;

        TimeExperiment settings;

        int counter = 0;
      

        struct TimeJumpPDR : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoals;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            public ComponentDataArray<AgentStep> AgentStep;
            public int TimeJump;


            public void Execute(int index)
            {
                float y = AgentStep[index].delta.y;
                float3 moveStep = AgentStep[index].delta * TimeJump;
                moveStep.y = y;
                AgentStep[index] = new AgentStep() { delta = moveStep };
                

            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            settings = TimeMachineSettings.experiment;

            if (!settings.Enabled)
                this.Enabled = false;

            counter = 0;

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            counter++;

            if (counter == settings.StartFrame)
            {
                var TimeJumpJob = new TimeJumpPDR()
                {
                    AgentGoals = agentGroup.AgentGoal,
                    AgentPos = agentGroup.Position,
                    AgentStep = agentGroup.AgentStep,
                    TimeJump = settings.FrameLeap
                };

                var TimeJumpJobDeps = TimeJumpJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

                Debug.Log("Jumped");
                TimeJumpJobDeps.Complete();
                return TimeJumpJobDeps;

            }
           
            return inputDeps;
        }
    }

}



