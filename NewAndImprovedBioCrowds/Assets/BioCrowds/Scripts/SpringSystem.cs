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
    [DisableAutoCreation]
    [UpdateAfter(typeof(AgentMovementVectors))]
    [UpdateBefore(typeof(FluidParticleToCell))]
    [UpdateAfter(typeof(AgentMassMapSystem))]
    public class SpringSystem : JobComponentSystem
    {

        public float InitialK = -500f;
        public float InitialKD = 3f;
        private float TimeStep = 0.0005f;

        [Inject] CellTagSystem cellTagSystem;
        [Inject] AgentMovementVectors agentMovementVectors;
        [Inject] AgentMassMapSystem m_AgentMassMap;

        public struct Spring
        {
            //ID of agent 1
            public int ID1;

            //ID of agent 2
            public int ID2;

            public float k;
            public float kd;

            public float l0;
        }

        
        public NativeList<Spring> springs;

        public NativeMultiHashMap<int, float3> AgentToForcesBeingApplied;

        public NativeHashMap<int, float3> AgentPosMap;
        public NativeHashMap<int, float3> AgentPosMap2;

        public NativeHashMap<int, float3> AgentStepMap;
        public NativeHashMap<int, float3> AgentStepMap2;


        protected override void OnStopRunning()
        {
            springs.Dispose();
            AgentToForcesBeingApplied.Dispose();
        }


        protected override void OnStartRunning()
        {

            AgentToForcesBeingApplied = new NativeMultiHashMap<int, float3>(Settings.agentQuantity * 5, Allocator.Persistent);
            springs = new NativeList<Spring>(Settings.experiment.SpringConnections.Length, Allocator.Persistent);

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

                //BOTA EM M1
                AgentToForcesBeingApplied.Add(ag1, (float)(-scalar + dampingScalar) * dir);




            }
        }

        struct ApplySpringForces : IJobParallelFor
        {

            [ReadOnly] public NativeHashMap<int, float> AgentMassMap;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public NativeMultiHashMap<int, float3> AgentToForcesBeingApplied;
            [NativeDisableParallelForRestriction]
            public NativeHashMap<int, float3> AgentIDToPos;
            [NativeDisableParallelForRestriction]
            public NativeHashMap<int, float3>.Concurrent AgentIDToPos2;
            [NativeDisableParallelForRestriction]
            public NativeHashMap<int, float3> AgentIDToStep;
            [NativeDisableParallelForRestriction]
            public NativeHashMap<int, float3>.Concurrent AgentIDToStep2;

            public ComponentDataArray<AgentStep> AgentStep;
            public float TimeStep;

            //0-qtdAgents
            public void Execute(int index)
            {
                int id = AgentData[index].ID;
                AgentMassMap.TryGetValue(id, out float mass);

                float3 currPos;
                AgentIDToPos.TryGetValue(id, out currPos);

                float3 currVel;
                AgentIDToStep.TryGetValue(id, out currVel);

                bool keepgoing = AgentToForcesBeingApplied.TryGetFirstValue(id, out float3 force, out NativeMultiHashMapIterator<int> it);
                if (!keepgoing) return;

                float3 F = force;

                while (AgentToForcesBeingApplied.TryGetNextValue(out force, ref it))
                {
                    F += force;
                }


                float3 a = F / mass;

                currVel += a * TimeStep;
                AgentIDToStep.Remove(id);
                bool b = AgentIDToStep2.TryAdd(id, currVel);
                if (!b) Debug.Log("AAAAAAAA");
                AgentStep[index] = new AgentStep { delta = currVel };


                currPos += currVel * TimeStep;
                AgentIDToPos.Remove(id);
                b = AgentIDToPos2.TryAdd(id, currPos);
                if (!b) Debug.Log("BBBBBBBB");

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            int iters = (int)math.ceil((1f / Settings.experiment.FramesPerSecond) * TimeStep);

            AgentPosMap = cellTagSystem.AgentIDToPos;
            AgentPosMap2 = new NativeHashMap<int, float3>(AgentPosMap.Capacity, Allocator.TempJob);

            AgentStepMap = cellTagSystem.AgentIDToPos;
            AgentStepMap2 = new NativeHashMap<int, float3>(AgentPosMap.Capacity, Allocator.TempJob);




            for (int i = 0; i < iters; i++)
            {
                AgentToForcesBeingApplied.Clear();
                
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
                    AgentIDToPos2 = AgentPosMap2.ToConcurrent(),
                    AgentIDToStep2 = AgentStepMap2.ToConcurrent(),
                    AgentIDToStep = agentMovementVectors.AgentIDToStep,
                    AgentToForcesBeingApplied = AgentToForcesBeingApplied,
                    TimeStep = TimeStep,
                    AgentStep = agentGroup.AgentStep,
                    AgentData = agentGroup.AgentData,
                    AgentMassMap = m_AgentMassMap.AgentID2MassMap
                };



                var ApplyForcesJobHandle = ApplyForces.Schedule(agentGroup.Length, Settings.BatchSize, ComputeForcesHandle); 

                ApplyForcesJobHandle.Complete();

                var aux = AgentPosMap;
                AgentPosMap = AgentPosMap2;
                AgentPosMap2 = aux;

                var aux2 = AgentStepMap;
                AgentStepMap = AgentStepMap2;
                AgentStepMap2 = aux2;

            }

            if(iters  % 2  == 1)
            {
                var aux = AgentPosMap;
                AgentPosMap = AgentPosMap2;
                AgentPosMap2 = aux;

                var aux2 = AgentStepMap;
                AgentStepMap = AgentStepMap2;
                AgentStepMap2 = aux2;

            }

            AgentPosMap2.Dispose();
            AgentStepMap2.Dispose();



            return inputDeps;
        }
    }


    public struct CouplingComponent : IComponentData
    {
        public int MaxCouplings;
        public int CurrentCouplings;
        public float CouplingDistance;

    }
    //TODO DEFINE COUPLINGSYSTEM ORDER UUUUUUUUUUUUUUUUUUUUUUUURGENTLY
    [DisableAutoCreation]
    public class CouplingSystem : ComponentSystem
    {

        public struct CouplingGroup
        {
            public ComponentDataArray<Position> Position;
            public ComponentDataArray<CouplingComponent> CouplingData;
            public ComponentDataArray<AgentData> AgentData;

            [ReadOnly] public readonly int Length;
        }

        public NativeQueue<int2> springConnectionsCandidates;

        [Inject] CellTagSystem m_cellTagSystem;
        [Inject] SpringSystem m_springSystem;
        [Inject] public CouplingGroup CouplingData;


 
        public struct CouplingDecisionJob : IJobParallelFor
        {
            [ReadOnly] public NativeMultiHashMap<int3, int> CellToAgents;
            [ReadOnly] public NativeHashMap<int, float3> AgentToPosition;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<CouplingComponent> CouplingData;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [WriteOnly] public NativeQueue<int2>.Concurrent springConnectionsCandidates;

            public void Execute(int index)
            {
                int agent_id = AgentData[index].ID;
                float3 agent_position = Position[index].Value;
                int3 cell = new int3((int)math.floor(agent_position.x / 2.0f) * 2 + 1, 0,
                                     (int)math.floor(agent_position.z / 2.0f) * 2 + 1);

                float coupling_distance = CouplingData[index].CouplingDistance;
               


                NativeMultiHashMapIterator<int3> iter;
                if (!CellToAgents.TryGetFirstValue(cell, out int currentAgent, out iter)) return;

                float3 current_agent_pos;
                AgentToPosition.TryGetValue(currentAgent, out current_agent_pos);

                if (math.distance(agent_position, current_agent_pos) < coupling_distance &&
                        currentAgent != agent_id)
                {
                    springConnectionsCandidates.Enqueue(new int2(agent_id, currentAgent));
                }

                while (CellToAgents.TryGetNextValue(out currentAgent, ref iter))
                {
                    AgentToPosition.TryGetValue(currentAgent, out current_agent_pos);

                    if (math.distance(agent_position, current_agent_pos) < coupling_distance &&
                        currentAgent != agent_id)
                    {
                        springConnectionsCandidates.Enqueue(new int2(agent_id, currentAgent));
                    }
                }

            }
        }

        public struct CouplingEffector : IJob
        {
            public NativeList<SpringSystem.Spring> springs;
            [ReadOnly] public float k;
            [ReadOnly] public float kd;
            [ReadOnly] public float l0;
            public NativeQueue<int2> springConnectionsCandidates;
            public ComponentDataArray<CouplingComponent> CouplingData;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;


            private bool CheckInList(int2 cand, NativeList<SpringSystem.Spring> list)
            {
                foreach (SpringSystem.Spring elem in list.ToArray())
                {
                    if (elem.ID1 == cand.x && elem.ID2 == cand.y)
                        return true;
                }
                return false;
            }

            public void Execute()
            {
                //TODO CHECK HOW BIDIRECTIONAL SPRINGS SHOULD WORK
                while(springConnectionsCandidates.Count > 0)
                {
                    int2 candidate = springConnectionsCandidates.Dequeue();

                    //if (CheckInList(candidate, springs))
                    //    continue;
                    bool new_spring = true;
                    for(int j = 0; j < springs.Length; j++)
                    {
                        var elem = springs[j];
                        if (elem.ID1 == candidate.x && elem.ID2 == candidate.y || 
                            elem.ID2 == candidate.x && elem.ID1 == candidate.y)
                        {
                            new_spring = false;
                            break;
                        }
                        
                    }
                    if (!new_spring)
                        continue;

                    int id1 = -1;
                    int id2 = -1;

                    for( int i = 0; i < AgentData.Length; i++)
                    {
                        if (candidate.x == AgentData[i].ID)
                            id1 = i;
                        if (candidate.y == AgentData[i].ID)
                            id2 = i;
                    }

                    CouplingComponent coupling_data_id1 = CouplingData[id1];
                    CouplingComponent coupling_data_id2 = CouplingData[id2];

                    if (!(coupling_data_id1.CurrentCouplings < coupling_data_id1.MaxCouplings &&
                        coupling_data_id2.CurrentCouplings < coupling_data_id2.MaxCouplings))
                        continue;

                    springs.Add(new SpringSystem.Spring
                    {
                        ID1 = AgentData[id1].ID,
                        ID2 = AgentData[id2].ID,
                        k = k,
                        kd = kd,
                        l0 = l0
                    });

                    CouplingData[id1] = new CouplingComponent
                    {
                        CouplingDistance = coupling_data_id1.CouplingDistance,
                        CurrentCouplings = coupling_data_id1.CurrentCouplings + 1,
                        MaxCouplings = coupling_data_id1.MaxCouplings
                    };

                    CouplingData[id2] = new CouplingComponent
                    {
                        CouplingDistance = coupling_data_id2.CouplingDistance,
                        CurrentCouplings = coupling_data_id2.CurrentCouplings + 1,
                        MaxCouplings = coupling_data_id2.MaxCouplings
                    };


                }



            }
        }


        protected override void OnUpdate()
        {
            // Dynamic Coupling Conditions

            var getCandidatesJob = new CouplingDecisionJob
            {
                AgentData = CouplingData.AgentData,
                AgentToPosition = m_cellTagSystem.AgentIDToPos,
                CellToAgents = m_cellTagSystem.CellToMarkedAgents,
                CouplingData = CouplingData.CouplingData,
                Position = CouplingData.Position,
                springConnectionsCandidates = springConnectionsCandidates.ToConcurrent()
            };

            var handle = getCandidatesJob.Schedule(CouplingData.Length, Settings.BatchSize);

            handle.Complete();

            var effectSpringsJob = new CouplingEffector
            {
                AgentData = CouplingData.AgentData,
                k = m_springSystem.InitialK,
                kd = m_springSystem.InitialKD,
                l0 = 1f,
                CouplingData = CouplingData.CouplingData,
                springConnectionsCandidates = springConnectionsCandidates,
                springs = m_springSystem.springs
            };

            var effector_handle = effectSpringsJob.Schedule();

            effector_handle.Complete();

        }

        protected override void OnStartRunning()
        {
            springConnectionsCandidates = new NativeQueue<int2>(Allocator.Persistent);

            foreach (int2 s in Settings.experiment.SpringConnections)
            {
                m_springSystem.springs.Add(new SpringSystem.Spring { k = m_springSystem.InitialK, kd = m_springSystem.InitialKD, ID1 = s.x, ID2 = s.y, l0 = 1f });
            }
        }


    }
}