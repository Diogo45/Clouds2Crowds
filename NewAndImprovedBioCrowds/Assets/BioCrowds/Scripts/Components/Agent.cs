using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine.Experimental.PlayerLoop;

namespace BioCrowds
{

    [System.Serializable]
    public struct AgentData : IComponentData
    {
        public int ID;
        public float Radius;
        public float MaxSpeed;
    }

    [System.Serializable]
    public struct AgentGoal : IComponentData
    {
        public float3 EndGoal;
        public float3 SubGoal;
    }

    [System.Serializable]
    public struct AgentStep : IComponentData
    {
        public float3 delta;
    }

    public struct Active: IComponentData
    {
        public int active;
    }



    [UpdateAfter(typeof(EarlyUpdate))]
    public class CellTagSystem : JobComponentSystem
    {
        
        public NativeMultiHashMap<int3, int> CellToMarkedAgents;
        public NativeHashMap<int, float3> AgentIDToPos;
        
        public struct AgentGroup
        {
            public ComponentDataArray<CellName> MyCell;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public readonly int Length;
        }
        [Inject] AgentGroup agentGroup;

        struct MapCellToAgents : IJobParallelFor
        {
            [WriteOnly] public NativeMultiHashMap<int3, int>.Concurrent CellToAgent;
            [WriteOnly] public NativeHashMap<int, float3>.Concurrent AgentIDToPos;

            [ReadOnly] public ComponentDataArray<Position> Position;
            public ComponentDataArray<CellName> MyCell;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;

            public void Execute(int index)
            {

                //Get the 8 neighbors cells to the agent's cell + it's cell
                int agent = AgentData[index].ID;
                int3 cell = MyCell[index].Value;
                
                CellToAgent.Add(cell, agent);
                int startX = MyCell[index].Value.x - 2;
                int startZ = MyCell[index].Value.z - 2;
                int endX = MyCell[index].Value.x + 2;
                int endZ = MyCell[index].Value.z + 2;

                float3 agentPos = Position[index].Value;
                AgentIDToPos.TryAdd(agent, agentPos);
                float distCell = math.distance((float3)MyCell[index].Value, agentPos);


                for (int i = startX; i <= endX; i = i + 2)
                {
                    for (int j = startZ; j <= endZ; j = j + 2)
                    {
                        int3 key = new int3(i, 0, j);
                        
                        CellToAgent.Add(key, agent);
                        float distNewCell = math.distance((float3)key, agentPos);
                        if (distNewCell < distCell)
                        {
                            distCell = distNewCell;
                            MyCell[index] = new CellName { Value = key };
                            //Debug.Log(cell + " " + key);
                        }
                    }
                }

            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            CellToMarkedAgents.Clear();
            AgentIDToPos.Clear();


            MapCellToAgents mapCellToAgentsJob = new MapCellToAgents
            {
                CellToAgent = CellToMarkedAgents.ToConcurrent(),
                AgentData = agentGroup.AgentData,
                MyCell = agentGroup.MyCell,
                Position = agentGroup.AgentPos,
                AgentIDToPos = AgentIDToPos.ToConcurrent()
            };

            var mapCellToAgentsJobDep = mapCellToAgentsJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

            mapCellToAgentsJobDep.Complete();

            //Debug.Log(AgentIDToPos.Length);
           
            return mapCellToAgentsJobDep;
        }

        protected override void OnStartRunning()
        {
            int qtdAgts = Settings.agentQuantity;
            CellToMarkedAgents = new NativeMultiHashMap<int3, int>(160000, Allocator.Persistent);

            AgentIDToPos = new NativeHashMap<int, float3>(qtdAgts * 2, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            CellToMarkedAgents.Dispose();
            AgentIDToPos.Dispose();
        }
    }


    public class MarkerSystemGroup { }

    public class MarkerSystemView : ComponentSystem
    {

        [Inject] MarkerSystem mS;
        [Inject] NormalLifeMarkerSystem nlmS;

        public NativeMultiHashMap<int, float3> AgentMarkers;


        protected override void OnUpdate()
        {

            if (nlmS.Enabled) AgentMarkers = nlmS.AgentMarkers;
            if (mS.Enabled) AgentMarkers = mS.AgentMarkers;
        }
    }


    [UpdateAfter(typeof(MarkerSystemGroup)), UpdateAfter(typeof(MarkerSystemView))]
    public class MarkerWeightSystem : JobComponentSystem
    {

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public readonly int Length;
        }
        [Inject] AgentGroup agentGroup;

        [Inject] MarkerSystemView MarkerSystem;

        public NativeHashMap<int, float> AgentTotalMarkerWeight;

        public struct ComputeTotalMarkerWeight : IJobParallelFor
        {
            [WriteOnly] public NativeHashMap<int, float>.Concurrent AgentTotalMarkerWeight;

            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoals;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public NativeMultiHashMap<int, float3> AgentMarkers;


            public void Execute(int index)
            {
                float3 currentMarkerPosition;
                NativeMultiHashMapIterator<int> it;

                float totalW = 0f;

                bool keepgoing = AgentMarkers.TryGetFirstValue(AgentData[index].ID, out currentMarkerPosition, out it);

                if (!keepgoing)
                    return;

                totalW += AgentCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, (AgentGoals[index].SubGoal - AgentPos[index].Value));

                while (AgentMarkers.TryGetNextValue(out currentMarkerPosition, ref it))
                    totalW += AgentCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, (AgentGoals[index].SubGoal - AgentPos[index].Value));

                AgentTotalMarkerWeight.TryAdd(AgentData[index].ID, totalW);

            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            AgentTotalMarkerWeight.Clear();
            
            ComputeTotalMarkerWeight computeJob = new ComputeTotalMarkerWeight()
            {
                AgentTotalMarkerWeight = AgentTotalMarkerWeight.ToConcurrent(),
                AgentData = agentGroup.AgentData,
                AgentGoals = agentGroup.AgentGoal,
                AgentPos = agentGroup.AgentPos,
                AgentMarkers = MarkerSystem.AgentMarkers
            };
            var computeJobHandle = computeJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);
            computeJobHandle.Complete();
            return computeJobHandle;
        }

        protected override void OnCreateManager()
        {
            //AgentTotalMarkerWeight = new NativeHashMap<int, float>();
        }

        protected override void OnStartRunning()
        {
            UpdateInjectedComponentGroups();
            AgentTotalMarkerWeight = new NativeHashMap<int, float>(agentGroup.Length * 2, Allocator.Persistent);
        }

        protected override void OnStopRunning()
        {
            AgentTotalMarkerWeight.Dispose();
        }

    }


    public class MovementVectorsSystemGroup { }

    
    [UpdateInGroup(typeof(MovementVectorsSystemGroup)), UpdateAfter(typeof(MarkerWeightSystem))]
    public class AgentMovementVectors : JobComponentSystem
    {
        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [WriteOnly] public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public readonly int Length;
        }


        [Inject] AgentGroup agentGroup;
        [Inject] MarkerSystem markerSystem;

        [Inject] MarkerWeightSystem totalWeightSystem;

        struct CalculateAgentMoveStep : IJobParallelFor
        {

            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoals;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public NativeMultiHashMap<int, float3> AgentMarkersMap;
            [ReadOnly] public NativeHashMap<int, float> AgentTotalW;
            [WriteOnly] public ComponentDataArray<AgentStep> AgentStep;


            public void Execute(int index)
            {
                float3 currentMarkerPosition;
                NativeMultiHashMapIterator<int> it;

                float3 moveStep = float3.zero;
                float3 direction = float3.zero;
                float totalW;
                AgentTotalW.TryGetValue(AgentData[index].ID, out totalW);

                bool keepgoing = AgentMarkersMap.TryGetFirstValue(AgentData[index].ID, out currentMarkerPosition, out it);

                if (!keepgoing)
                    return;

                float F = AgentCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, AgentGoals[index].SubGoal - AgentPos[index].Value);
                direction += AgentCalculations.PartialW(totalW, F) * AgentData[index].MaxSpeed * (currentMarkerPosition - AgentPos[index].Value);

                while (AgentMarkersMap.TryGetNextValue(out currentMarkerPosition, ref it))
                {
                    F = AgentCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, AgentGoals[index].SubGoal - AgentPos[index].Value);
                    direction += AgentCalculations.PartialW(totalW, F) * AgentData[index].MaxSpeed * (currentMarkerPosition - AgentPos[index].Value);
                }


                float moduleM = math.length(direction);
                float s = (float)(moduleM * math.PI);

                if (s > AgentData[index].MaxSpeed)
                    s = AgentData[index].MaxSpeed;

                if (moduleM > 0.00001f)
                    moveStep = s * (math.normalize(direction));
                else
                    moveStep = float3.zero;

                AgentStep[index] = new AgentStep() { delta = moveStep };

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            var calculateMoveStepJob = new CalculateAgentMoveStep()
            {
                AgentData = agentGroup.AgentData,
                AgentGoals = agentGroup.AgentGoal,
                AgentPos = agentGroup.Position,
                AgentStep = agentGroup.AgentStep,
                AgentTotalW = totalWeightSystem.AgentTotalMarkerWeight,
                AgentMarkersMap = markerSystem.AgentMarkers
            };

            var calculateMoveStepDeps = calculateMoveStepJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

            calculateMoveStepDeps.Complete();

            return calculateMoveStepDeps;
        }

    }


    [UpdateAfter(typeof(AgentMovementVectors))]
    public class AgentMovementSystem : JobComponentSystem
    {
        //Moves based on marked cell list
        public struct MarkersGroup
        {
            [WriteOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public readonly int Length;
        }
        [Inject] MarkersGroup markersGroup;

        struct MoveCloudsJob : IJobParallelFor
        {
            public ComponentDataArray<Position> Positions;
            [ReadOnly] public ComponentDataArray<AgentStep> Deltas;

            public void Execute(int index)
            {
                float3 old = Positions[index].Value;
                //Debug.Log(Deltas[index].Delta);

                Positions[index] = new Position { Value = old + Deltas[index].delta };
            }
        }




        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            MoveCloudsJob moveJob = new MoveCloudsJob()
            {
                Positions = markersGroup.Position,
                Deltas = markersGroup.AgentStep
            };

            var deps = moveJob.Schedule(markersGroup.Length, Settings.BatchSize, inputDeps);

            deps.Complete();

            return deps;
        }

    }



    

    public static class AgentCalculations
    {
        //Current marker position, current cloud position and (goal position - cloud position) vector.
        public static float GetF(float3 markerPosition, float3 agentPosition, float3 agentGoalVector)
        {
            float Ymodule = math.length(markerPosition - agentPosition);

            float Xmodule = 1f;

            float dot = math.dot(markerPosition - agentPosition, math.normalize(agentGoalVector));

            if (Ymodule < 0.00001f)
                return 0.0f;

            return ((1.0f / (1.0f + Ymodule)) * (1.0f + ((dot) / (Xmodule * Ymodule))));
        }

        public static float PartialW(float totalW, float fValue)
        {
            return fValue / totalW;
        }
    }

}