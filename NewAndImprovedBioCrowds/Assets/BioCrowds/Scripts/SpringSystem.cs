using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace BioCrowds
{
    [UpdateAfter(typeof(AgentMovementVectors))]
    [UpdateBefore(typeof(FluidParticleToCell))]
    public class SpringSystem : JobComponentSystem
    {

        private float InitialK = -500f;
        private float InitialKD = 3f;
        private float TimeStep = 0.0005f;

        [Inject] CellTagSystem cellTagSystem;
        [Inject] AgentMovementVectors agentMovementVectors;

        public struct Spring
        {
            //ID of agent 1
            public int ID1;
            //Agent 1 mass
            public float M1;
            //ID of agent 2
            public int ID2;
            //Agent 2 mass
            public float M2;

            public float k;
            public float kd;

            public float l0;
        }

        

        public NativeList<Spring> springs;

        public NativeMultiHashMap<int, float3> AgentToForcesBeingApplied;

        protected override void OnStopRunning()
        {
            springs.Dispose();
            AgentToForcesBeingApplied.Dispose();
        }


        protected override void OnStartRunning()
        {
            springs = new NativeList<Spring>(Settings.experiment.SpringConnections.Length, Allocator.Persistent);

            AgentToForcesBeingApplied = new NativeMultiHashMap<int, float3>(springs.Capacity * Settings.agentQuantity, Allocator.Persistent);
            foreach (int2 s in Settings.experiment.SpringConnections)
            {
                springs.Add(new Spring { k = InitialK, kd = InitialKD, ID1 = s.x, ID2 = s.y, M1 = 70f, M2 = 70f, l0 = 1f });
            }


        }

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public readonly int Length;
        }
        [Inject] AgentGroup agentGroup;

        struct SolveSpringForces : IJobParallelFor
        {


            [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;
            [ReadOnly] public NativeHashMap<int, float3> AgentIDToStep;
            [ReadOnly] public NativeList<Spring> springs;

            [WriteOnly] public NativeMultiHashMap<int, float3>.Concurrent AgentToForcesBeingApplied;


            public void Execute(int index)
            {

                int ag1 = springs[index].ID1;
                int ag2 = springs[index].ID2;
                float k = springs[index].k;
                float kd = springs[index].kd;
                float l0 = springs[index].l0;

                float3 p1;
                AgentIDToPos.TryGetValue(ag1, out p1);
                float3 p2;
                AgentIDToPos.TryGetValue(ag2, out p2);

                float3 v1;
                AgentIDToStep.TryGetValue(ag1, out v1);
                float3 v2;
                AgentIDToStep.TryGetValue(ag2, out v2);


                //spring force
                double delta = math.distance(p1, p2);
                double scalar = k * (delta - l0);
                Vector3 dir = (p1 - p2);

                dir.Normalize();

                //Damping
                double s1 = math.dot(v1, dir);
                double s2 = math.dot(v2, dir);
                double dampingScalar = -kd * (s1 + s2);

                //BOTA EM M2
                AgentToForcesBeingApplied.Add(ag2, (float)(-scalar + dampingScalar) * dir);

            }
        }

        struct ApplySpringForces : IJobParallelFor
        {
            [ReadOnly] public NativeMultiHashMap<int, float3> AgentToForcesBeingApplied;
            [NativeDisableParallelForRestriction]
            public NativeHashMap<int, float3> AgentIDToPos;
            [NativeDisableParallelForRestriction]
            public NativeHashMap<int, float3> AgentIDToStep;
            public ComponentDataArray<AgentStep> AgentStep;
            public float TimeStep;

            //0-qtdAgents
            public void Execute(int index)
            {
                float3 currPos;
                AgentIDToPos.TryGetValue(index, out currPos);
                float3 currVel;
                AgentIDToStep.TryGetValue(index, out currVel);

                bool keepgoing = AgentToForcesBeingApplied.TryGetFirstValue(index, out float3 force, out  NativeMultiHashMapIterator<int> it);
                if (!keepgoing) return;

                float3 F = force;

                while(AgentToForcesBeingApplied.TryGetNextValue(out force, ref it))
                {
                    F += force;
                }


                float3 a = F / 70f;

                currVel += a * TimeStep;
                AgentIDToStep.Remove(index);
                bool b = AgentIDToStep.TryAdd(index, currVel);
                if (!b) Debug.Log("AAAAAAAA");
                AgentStep[index] = new AgentStep { delta = currVel };


                currPos += currVel * TimeStep;
                AgentIDToPos.Remove(index);
                b = AgentIDToPos.TryAdd(index, currPos);
                if (!b) Debug.Log("BBBBBBBB");

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            int iters = (int)math.ceil((1f/ Settings.experiment.FramesPerSecond) * TimeStep);

            for (int i = 0; i < iters; i++)
            {
                AgentToForcesBeingApplied.Clear();
                //Debug.Log("CLEAR");
                var ComputeForces = new SolveSpringForces
                {
                    AgentIDToPos = cellTagSystem.AgentIDToPos,
                    AgentIDToStep = agentMovementVectors.AgentIDToStep,
                    AgentToForcesBeingApplied = AgentToForcesBeingApplied.ToConcurrent(),
                    springs = springs
                };

                var ComputeForcesHandle = ComputeForces.Schedule(springs.Length, Settings.BatchSize, inputDeps);

                ComputeForcesHandle.Complete();

                var ApplyForces = new ApplySpringForces
                {
                    AgentIDToPos = cellTagSystem.AgentIDToPos,
                    AgentIDToStep = agentMovementVectors.AgentIDToStep,
                    AgentToForcesBeingApplied = AgentToForcesBeingApplied,
                    TimeStep = TimeStep,
                    AgentStep = agentGroup.AgentStep
                };

                

                var ApplyForcesJobHandle = ApplyForces.Schedule(agentGroup.Length, Settings.BatchSize, ComputeForcesHandle);

                ApplyForcesJobHandle.Complete();
                //Debug.Log("a");

            }



            return inputDeps;
        }




    }
}