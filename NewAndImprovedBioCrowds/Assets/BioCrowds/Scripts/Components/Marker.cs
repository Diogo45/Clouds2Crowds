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


    [UpdateAfter(typeof(MarkerSystem))]
    public class MarkerBarrier : BarrierSystem { }

    [UpdateAfter(typeof(CellTagSystem))]
    [UpdateInGroup(typeof(MarkerSystemGroup))]
    public class MarkerSystem : JobComponentSystem
    {
        [Inject] private MarkerBarrier m_SpawnerBarrier;


        [Inject] public CellTagSystem CellTagSystem;

        public NativeMultiHashMap<int, float3> AgentMarkers;

        public struct MarkerGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<MarkerData> Data;
            [ReadOnly] public ComponentDataArray<Active> Active;
            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public ComponentDataArray<CellName> MarkerCell;
            [ReadOnly] public readonly int Length;
        }
        [Inject] MarkerGroup markerGroup;


        struct TakeMarkers : IJobParallelFor
        {
            [WriteOnly] public NativeMultiHashMap<int, float3>.Concurrent AgentMarkers;
            [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;
            [ReadOnly] public NativeMultiHashMap<int3, int> cellToAgent;
            public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public EntityArray Entities;

            [ReadOnly] public ComponentDataArray<CellName> MarkerCell;
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

                    return;
                }
                

                float3 agentPos;
                AgentIDToPos.TryGetValue(currentAgent, out agentPos);

                float dist = math.distance(MarkerPos[index].Value, agentPos);

                if(dist < agentRadius)
                {
                    closestDistance = dist;
                    bestAgent = currentAgent;
                }

                while(cellToAgent.TryGetNextValue(out currentAgent, ref iter))
                {
                    AgentIDToPos.TryGetValue(currentAgent, out agentPos);
                    dist = math.distance(MarkerPos[index].Value, agentPos);
                    if(dist < agentRadius && dist <= closestDistance)
                    {
                        if(dist != closestDistance)
                        {
                            closestDistance = dist;
                            bestAgent = currentAgent;
                        }
                        else
                        {
                            if(bestAgent > currentAgent)
                            {
                                bestAgent = currentAgent;
                                closestDistance = dist;
                            }
                        }
                    }


                }

                if (bestAgent == -1) return;
                //Debug.Log(bestAgent);
                //CommandBuffer.AddComponent(index, Entities[index], new Active { active = 1 });
                AgentMarkers.Add(bestAgent, MarkerPos[index].Value);

            }

        }



        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            AgentMarkers.Clear();


            int qtdAgents = Settings.agentQuantity;

            TakeMarkers takeMarkersJob = new TakeMarkers
            {
                AgentIDToPos = CellTagSystem.AgentIDToPos,
                AgentMarkers = AgentMarkers.ToConcurrent(),
                cellToAgent = CellTagSystem.CellToMarkedAgents,
                MarkerCell = markerGroup.MarkerCell,
                MarkerPos = markerGroup.Position,
                Entities = markerGroup.Entities,
                CommandBuffer = m_SpawnerBarrier.CreateCommandBuffer().ToConcurrent()
            };


            var takeMakersHandle = takeMarkersJob.Schedule(markerGroup.Length, Settings.BatchSize, inputDeps);
            takeMakersHandle.Complete();

            

            return takeMakersHandle;
        }


        protected override void OnStartRunning()
        {
            UpdateInjectedComponentGroups();
            int qtdAgents = Settings.agentQuantity;
            float densityToQtd = Settings.instance.MarkerDensity / Mathf.Pow(Settings.instance.markerRadius, 2f);
            int qtdMarkers = Mathf.FloorToInt(densityToQtd);
            
            AgentMarkers = new NativeMultiHashMap<int, float3>(qtdAgents * qtdMarkers * 4, Allocator.TempJob);
           
        }

        protected override void OnStopRunning()
        {
            AgentMarkers.Dispose();
        }

    }


  


}