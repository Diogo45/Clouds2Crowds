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

        public NativeHashMap<int3, float> LocalDensities;

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentData> Agents;
            [ReadOnly] public ComponentDataArray<Position> Positions;
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
            [WriteOnly] public NativeHashMap<int3, float>.Concurrent Densities;
            [WriteOnly] public NativeMultiHashMap<int, float3>.Concurrent AgentMarkers;
            [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;
            [ReadOnly] public NativeMultiHashMap<int3, int> cellToAgent;
            [ReadOnly] public SharedComponentDataArray<MarkerCellName> MarkerCell;
            [ReadOnly] public ComponentDataArray<Position> MarkerPos;

            public void Execute(int index)
            {
                NativeMultiHashMapIterator<int3> iter;
                int agents = 0;
                //int3 currentCell;
                int currentAgent = -1;
                int bestAgent = -1;
                float agentRadius = 1f;
                float closestDistance = agentRadius + 1;

                bool keepgoing = cellToAgent.TryGetFirstValue(MarkerCell[index].Value, out currentAgent, out iter);


                if (!keepgoing)
                {
                    return;
                }
                

                float3 agentPos;
                AgentIDToPos.TryGetValue(currentAgent, out agentPos);

                agents++;

                float dist = math.distance(MarkerPos[index].Value, agentPos);

                if (dist < agentRadius)
                {
                    closestDistance = dist;
                    bestAgent = currentAgent;
                }

                while (cellToAgent.TryGetNextValue(out currentAgent, ref iter))
                {
                    agents++;
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

                Densities.TryAdd(MarkerCell[index].Value, (float)agents / 36f);
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
                MarkerPos = markerGroup.Position,
                Densities = LocalDensities.ToConcurrent()
                //Entities = markerGroup.Entities,
                //CommandBuffer = m_SpawnerBarrier.CreateCommandBuffer().ToConcurrent()
            };
            
            //if (CellTagSystem.AgentIDToPos.IsCreated)
           // {
                JobHandle takeMakersHandle = takeMarkersJob.Schedule(markerGroup.Length, Settings.BatchSize, inputDeps);
                takeMakersHandle.Complete();


                NativeMultiHashMap<int, float3> agtM = AgentMarkers;

                NativeMultiHashMapIterator<int> iter;
                float3 marker;
                for (int i = 0; i < agentGroup.Length; i++)
                {
                    bool keepGoing = agtM.TryGetFirstValue(agentGroup.Agents[i].ID, out marker, out iter);
                    if (keepGoing)
                    {
                        //Debug.Log(i);

                        Debug.DrawLine(agentGroup.Positions[i].Value, marker);
                        while (agtM.TryGetNextValue(out marker, ref iter))
                        {
                            Debug.DrawLine(agentGroup.Positions[i].Value, marker);

                        }

                    }

                }


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
            LocalDensities = new NativeHashMap<int3, float>(markerGroup.Length, Allocator.Persistent);

        }

        protected override void OnStopRunning()
        {
            AgentMarkers.Dispose();
            LocalDensities.Dispose();
        }

    }

    [UpdateAfter(typeof(CellTagSystem))]
    [UpdateInGroup(typeof(MarkerSystemGroup))]
    public class MarkerSystemMk2 : JobComponentSystem
    {
        private bool createCells = true;

        private int qtdMarkers = 0;

        [Inject] private m_SpawnerBarrier m_SpawnerBarrier;

        [Inject] public CellTagSystem CellTagSystem;

        public NativeMultiHashMap<int, float3> AgentMarkers;
        private Dictionary<int, float3[]> cellMarkers;
        QuadTree qt;

        public struct MarkerGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<MarkerData> Data;
            [ReadOnly] public SharedComponentDataArray<MarkerCellName> MarkerCell;
            [ReadOnly] public readonly int Length;
        }
        [Inject] MarkerGroup markerGroup;

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentData> Agents;
            [ReadOnly] public ComponentDataArray<Position> Positions;
            [ReadOnly] public readonly int Length;
        }
        [Inject] AgentGroup agentGroup;


        struct TakeMarkers : IJobParallelFor
        {

            [ReadOnly] public Dictionary<int, float3[]> cellMarkers;
            [ReadOnly] public List<int3> cells;
            [WriteOnly] public NativeMultiHashMap<int, float3> AgentMarkers;




            public void Execute(int index)
            {
                throw new System.NotImplementedException();
            }
        }

       


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            qt = CellTagSystem.qt;
            if (createCells)
            {
                CreateCells();
                createCells = false;
            }

            //Get QuadTree quadrants that need to be schedueled
            var schedule = qt.GetScheduled();
            //list<quadrants> --> [[cell1, cell2, ..., celln], [celln+1, ...], ...]
            //[cell1, cell2, ..., celln] --> ComponentDataArray<Position>
            //Job <-- Position, MarkedCells { markerCell --> checkAgents } 
            




            return base.OnUpdate(inputDeps);
        }

        private void CreateCells()
        {
            
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