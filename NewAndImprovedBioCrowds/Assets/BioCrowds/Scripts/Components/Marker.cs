using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;
using Unity.Transforms;
using Unity.Jobs;
using UnityEditor;

namespace BioCrowds
{


    [System.Serializable]
    public struct MarkerData : IComponentData
    {
        //Marker ID for inserting in the agent's list, going from 0 to MarkerDensityCell / MarkerRadius². Where MarkerDensityCell is the density of markers in each cell and MarkerRadius is the radius for markers to collide.
        public int id;


        //The id of the agent who took this marker
        public int agtID;
    }


    [UpdateAfter(typeof(CellTagSystem))]
    [UpdateInGroup(typeof(MarkerSystemGroup))]
    public class MarkerSystem : JobComponentSystem
    {

        private int qtdMarkers = 0;

        [Inject] private m_SpawnerBarrier m_SpawnerBarrier;

        [Inject] public CellTagSystem CellTagSystem;

        public NativeMultiHashMap<int, float3> AgentMarkers;

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentData> Agents;
            [ReadOnly] public readonly int Length;

        }

        public struct MarkerGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<MarkerData> Data;
            //[ReadOnly] public ComponentDataArray<Active> Active;
            //[ReadOnly] public EntityArray Entities;
            [ReadOnly] public SharedComponentDataArray<MarkerCellName> MarkerCell;
            [ReadOnly] public readonly int Length;
        }
         
   
        [Inject] MarkerGroup markerGroup;
        [Inject] AgentGroup agentGroup;

        struct TakeMarkers : IJobParallelFor
        {
            [WriteOnly] public NativeMultiHashMap<int, float3>.Concurrent AgentMarkers;
            [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;
            [ReadOnly] public NativeMultiHashMap<int3, int> cellToAgent;
            //public EntityCommandBuffer.Concurrent CommandBuffer;
            //[ReadOnly] public EntityArray Entities;
            [ReadOnly] public SharedComponentDataArray<MarkerCellName> MarkerCell;
            [ReadOnly] public ComponentDataArray<Position> MarkerPos;

            public void Execute(int index)
            {
                NativeMultiHashMapIterator<int3> iter;

                //int3 currentCell;
                int currentAgent = -1;
                int bestAgent = -1;
                float agentRadius = 1f;
                float closestDistance = agentRadius + 1;

                bool keepgoing = cellToAgent.TryGetFirstValue(MarkerCell[index].Value, out currentAgent, out iter);


                if (!keepgoing)
                {
                    //CommandBuffer.RemoveComponent(index, Entities[index], typeof(Active));

                    //CommandBuffer.RemoveComponent(index, Entities[index], typeof(Active));
                    return;
                }

                //Debug.Log("Passou: " + MarkerCell[index].Value);

                float3 agentPos;
                AgentIDToPos.TryGetValue(currentAgent, out agentPos);

                float dist = math.distance(MarkerPos[index].Value, agentPos);

                if (dist < agentRadius)
                {
                    closestDistance = dist;
                    bestAgent = currentAgent;
                }

                while (cellToAgent.TryGetNextValue(out currentAgent, ref iter))
                {
                    AgentIDToPos.TryGetValue(currentAgent, out agentPos);
                    dist = math.distance(MarkerPos[index].Value, agentPos);
                    if (dist < agentRadius && dist <= closestDistance)
                    {
                        if (dist != closestDistance)
                        {
                            closestDistance = dist;
                            bestAgent = currentAgent;
                            //Debug.Log(MarkerCell[index].Value + " " + bestAgent);
                        }
                        else
                        {
                            if (bestAgent > currentAgent)
                            {
                                bestAgent = currentAgent;
                                //Debug.Log(MarkerCell[index].Value + " " + bestAgent);

                                closestDistance = dist;
                            }
                        }
                    }


                }

                if (bestAgent == -1) return;
                //Debug.Log(bestAgent);
                AgentMarkers.Add(bestAgent, MarkerPos[index].Value);


            }

        }



        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            
            if(AgentMarkers.Capacity < agentGroup.Agents.Length * qtdMarkers * 4)
            {
                AgentMarkers.Dispose();
                AgentMarkers = new NativeMultiHashMap<int, float3>(agentGroup.Agents.Length * qtdMarkers * 4, Allocator.Persistent);
            }
            else
                AgentMarkers.Clear();


            if (!CellTagSystem.AgentIDToPos.IsCreated)
            {
                return inputDeps;
            }


            TakeMarkers takeMarkersJob = new TakeMarkers
            {
                AgentIDToPos = CellTagSystem.AgentIDToPos,
                AgentMarkers = AgentMarkers.ToConcurrent(),
                cellToAgent = CellTagSystem.CellToMarkedAgents,
                MarkerCell = markerGroup.MarkerCell,
                MarkerPos = markerGroup.Position
                //Entities = markerGroup.Entities,
                //CommandBuffer = m_SpawnerBarrier.CreateCommandBuffer().ToConcurrent()
            };
            
            //if (CellTagSystem.AgentIDToPos.IsCreated)
           // {
                JobHandle takeMakersHandle = takeMarkersJob.Schedule(markerGroup.Length, Settings.BatchSize, inputDeps);
                takeMakersHandle.Complete();
                return takeMakersHandle;
           // }
            
           // return inputDeps;
        }


        protected override void OnStartRunning()
        {
            UpdateInjectedComponentGroups();
            int qtdAgents = Settings.agentQuantity;
            float densityToQtd = Settings.experiment.MarkerDensity / Mathf.Pow(Settings.experiment.markerRadius, 2f);
            qtdMarkers = Mathf.FloorToInt(densityToQtd);

            AgentMarkers = new NativeMultiHashMap<int, float3>(agentGroup.Agents.Length * qtdMarkers * 4, Allocator.Persistent);

        }

        protected override void OnStopRunning()
        {
            AgentMarkers.Dispose();
        }

    }

    [UpdateAfter(typeof(MarkerSystemGroup))]
    public class m_SpawnerBarrier:BarrierSystem
    {
    }
}