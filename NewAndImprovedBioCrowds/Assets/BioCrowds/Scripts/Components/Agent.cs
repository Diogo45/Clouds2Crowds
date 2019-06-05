
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine;
using System;
using System.Collections.Generic;

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

    public struct Active : IComponentData
    {
        public int active;
    }



    [UpdateAfter(typeof(EarlyUpdate))]
    public class CellTagSystem : JobComponentSystem
    {
        public NativeHashMap<int, float3> AgentIDToPos;
        public NativeMultiHashMap<int3, int> CellToMarkedAgents;
        public QuadTree qt;

        public struct CellGroup
        {
            [ReadOnly] public ComponentDataArray<CellName> CellName;
            [ReadOnly] public SubtractiveComponent<AgentData> Agent;
            [ReadOnly] public SubtractiveComponent<MarkerData> Marker;

            [ReadOnly] public readonly int Length;
        }
        [Inject] public CellGroup cellGroup;

        public struct AgentGroup
        {
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
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;

            public void Execute(int index)
            {
                //Get the 8 neighbors cells to the agent's cell + it's cell
                int agent = AgentData[index].ID;
                int3 cell = new int3((int)math.floor(Position[index].Value.x / 2.0f) * 2 + 1, 0,
                                     (int)math.floor(Position[index].Value.z / 2.0f) * 2 + 1);

                CellToAgent.Add(cell, agent);
                int startX = cell.x - 2;
                int startZ = cell.z - 2;
                int endX = cell.x + 2;
                int endZ = cell.z + 2;

                float3 agentPos = Position[index].Value;
                AgentIDToPos.TryAdd(agent, agentPos);
                float distCell = math.distance((float3)cell, agentPos);

                for (int i = startX; i <= endX; i = i + 2)
                {
                    for (int j = startZ; j <= endZ; j = j + 2)
                    {
                        int3 key = new int3(i, 0, j);

                        CellToAgent.Add(key, agent);
                        
                    }
                }

            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //int qtdAgts = Settings.agentQuantity;
            qt.Reset();

            if (AgentIDToPos.Capacity < agentGroup.Length)
            {
                AgentIDToPos.Dispose();
                AgentIDToPos = new NativeHashMap<int, float3>(agentGroup.Length * 2, Allocator.Persistent);
            }
            else
                AgentIDToPos.Clear();

            CellToMarkedAgents.Clear();

            MapCellToAgents mapCellToAgentsJob = new MapCellToAgents
            {
                CellToAgent = CellToMarkedAgents.ToConcurrent(),
                AgentData = agentGroup.AgentData,
                Position = agentGroup.AgentPos,
                AgentIDToPos = AgentIDToPos.ToConcurrent()
            };

            var mapCellToAgentsJobDep = mapCellToAgentsJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

            mapCellToAgentsJobDep.Complete();

            List<int3> addedCells = new List<int3>();

            for(int i = 0; i < cellGroup.Length; i++)
            {
                int3 key = cellGroup.CellName[i].Value;
                int item;
                NativeMultiHashMapIterator<int3> it;
                if(CellToMarkedAgents.TryGetFirstValue(key, out item, out it))
                {
                    //Debug.Log(key);
                    qt.Insert(key);
                }

            }

            return mapCellToAgentsJobDep;
        }


        

        protected override void OnStartRunning()
        {
            Rectangle size = new Rectangle { x = 0, y = 0, h = Settings.experiment.TerrainZ, w = Settings.experiment.TerrainX };
            qt = new QuadTree(size, 0);
            ShowQuadTree.qt = qt;
            int qtdAgts = Settings.agentQuantity;
            //TODO dynamize
            CellToMarkedAgents = new NativeMultiHashMap<int3, int>(160000, Allocator.Persistent);
            //Debug.Log(CellToMarkedAgents.IsCreated);
            AgentIDToPos = new NativeHashMap<int, float3>(qtdAgts * 2, Allocator.Persistent);
        }

        protected override void OnStopRunning()
        {
            CellToMarkedAgents.Dispose();
            AgentIDToPos.Dispose();
        }
    }


    public class MarkerSystemGroup { }

    [UpdateAfter(typeof(MarkerSystemGroup))]
    public class MarkerSystemView : ComponentSystem
    {
        [Inject] MarkerSystemMk2 mk2;
        [Inject] MarkerSystem mS;
        [Inject] NormalLifeMarkerSystem nlmS;

        public NativeMultiHashMap<int, float3> AgentMarkers;


        protected override void OnUpdate()
        {
            if (mk2.Enabled) AgentMarkers = mk2.AgentMarkers;
            else if (nlmS.Enabled) AgentMarkers = nlmS.AgentMarkers;
            else if (mS.Enabled) AgentMarkers = mS.AgentMarkers;
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

            if(AgentTotalMarkerWeight.Capacity < agentGroup.Length * 2)
            {
                AgentTotalMarkerWeight.Dispose();
                AgentTotalMarkerWeight = new NativeHashMap<int, float>(agentGroup.Length * 2, Allocator.Persistent);
            }
            else
                AgentTotalMarkerWeight.Clear();

            if (!MarkerSystem.AgentMarkers.IsCreated) return inputDeps;

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


    [UpdateInGroup(typeof(MovementVectorsSystemGroup)), UpdateAfter(typeof(MarkerWeightSystem)), UpdateAfter(typeof(BioClouds.CellMarkSystem))]
    public class AgentMovementVectors : JobComponentSystem
    {
        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public SharedComponentDataArray<AgentCloudID> AgentCloudID;

            [ReadOnly] public ComponentDataArray<Position> Position;
            [WriteOnly] public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public readonly int Length;
        }


        [Inject] AgentGroup agentGroup;
        [Inject] MarkerSystemView markerSystem;

        [Inject] MarkerWeightSystem totalWeightSystem;

        //TODO BioClouds stuff
        [Inject] BioClouds.CellMarkSystem m_BioCloudsCellMarkSystem;
        [Inject] BioClouds.CloudCellTagSystem m_BioCloudsCellTagSystem;
        // end BIOCLOUDS

        struct CalculateAgentMoveStep : IJobParallelFor
        {

            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoals;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;

            [ReadOnly] public SharedComponentDataArray<AgentCloudID> AgentCloudID;

            [ReadOnly] public NativeMultiHashMap<int, float3> AgentMarkersMap;
            [ReadOnly] public NativeHashMap<int, float> AgentTotalW;
            [WriteOnly] public ComponentDataArray<AgentStep> AgentStep;


            public void Execute(int index)
            {

                BioClouds.CloudIDPosRadius CloudPos;


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

        struct CalculateAgentMoveStepCloudCohesion : IJobParallelFor
        {

            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoals;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;

            [ReadOnly] public SharedComponentDataArray<AgentCloudID> AgentCloudID;
            
            [ReadOnly] public NativeMultiHashMap<int, float3> AgentMarkersMap;
            [ReadOnly] public NativeHashMap<int, float> AgentTotalW;
            [WriteOnly] public ComponentDataArray<AgentStep> AgentStep;


            [ReadOnly] public NativeHashMap<int, BioClouds.CloudIDPosRadius> BioClouds2PosMap;
            [ReadOnly] public NativeHashMap<int, int> BioCloudsCell2OwningCloudMap;



            public void Execute(int index)
            {

                BioClouds.CloudIDPosRadius CloudPos;
                if (!BioClouds2PosMap.TryGetValue(AgentCloudID[index].CloudID, out CloudPos))
                    return;
                float3 CloudPosition = CloudPos.position;
                float3 BioCrowdsCloudPosition = WindowManager.Clouds2Crowds(CloudPosition);


                float3 Agent2CloudCenterVec = BioCrowdsCloudPosition - AgentPos[index].Value;

                float3 NormalizedAgent2CloudCenter = math.normalize(Agent2CloudCenterVec);


                float3 currentMarkerPosition;
                NativeMultiHashMapIterator<int> it;

                float3 moveStep = float3.zero;
                float3 direction = float3.zero;
                float totalW;
                AgentTotalW.TryGetValue(AgentData[index].ID, out totalW);

                bool keepgoing = AgentMarkersMap.TryGetFirstValue(AgentData[index].ID, out currentMarkerPosition, out it);

                if (!keepgoing)
                    return;

                float extraweight = math.dot(NormalizedAgent2CloudCenter, currentMarkerPosition - AgentPos[index].Value);

                float F = AgentCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, AgentGoals[index].SubGoal - AgentPos[index].Value);

                F += extraweight * 0.1f;

                direction += AgentCalculations.PartialW(totalW, F) * AgentData[index].MaxSpeed * (currentMarkerPosition - AgentPos[index].Value);



                while (AgentMarkersMap.TryGetNextValue(out currentMarkerPosition, ref it))
                {

                    extraweight = math.dot(NormalizedAgent2CloudCenter, currentMarkerPosition - AgentPos[index].Value);

                    F = AgentCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, AgentGoals[index].SubGoal - AgentPos[index].Value);

                    F += extraweight * 0.1f;

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


            if (Settings.experiment.BioCloudsEnabled)
            {
                var calculateMoveStepJob = new CalculateAgentMoveStepCloudCohesion()
                {
                    AgentData = agentGroup.AgentData,
                    AgentGoals = agentGroup.AgentGoal,
                    AgentPos = agentGroup.Position,
                    AgentStep = agentGroup.AgentStep,
                    AgentTotalW = totalWeightSystem.AgentTotalMarkerWeight,
                    AgentMarkersMap = markerSystem.AgentMarkers,
                    BioCloudsCell2OwningCloudMap = m_BioCloudsCellMarkSystem.Cell2OwningCloud,
                    BioClouds2PosMap = m_BioCloudsCellTagSystem.cloudIDPositions,
                    AgentCloudID = agentGroup.AgentCloudID
                };
                
                var calculateMoveStepDeps = calculateMoveStepJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

                calculateMoveStepDeps.Complete();

                return calculateMoveStepDeps;

            }
            else
            {
                var calculateMoveStepJob = new CalculateAgentMoveStep()
                {
                    AgentData = agentGroup.AgentData,
                    AgentGoals = agentGroup.AgentGoal,
                    AgentPos = agentGroup.Position,
                    AgentStep = agentGroup.AgentStep,
                    AgentTotalW = totalWeightSystem.AgentTotalMarkerWeight,
                    AgentMarkersMap = markerSystem.AgentMarkers,
                    AgentCloudID = agentGroup.AgentCloudID
                };
                
                var calculateMoveStepDeps = calculateMoveStepJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

                calculateMoveStepDeps.Complete();

                return calculateMoveStepDeps;
            }

            
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
            public ComponentDataArray<AgentGoal> Goal;
            [ReadOnly] public readonly int Length;
        }
        [Inject] MarkersGroup markersGroup;

        struct MoveCloudsJob : IJobParallelFor
        {
            public ComponentDataArray<Position> Positions;
            [ReadOnly] public ComponentDataArray<AgentStep> Deltas;
            public ComponentDataArray<AgentGoal> Goal;


            public void Execute(int index)
            {
                float3 old = Positions[index].Value;

                Positions[index] = new Position { Value = old + Deltas[index].delta };



                //DONOW:Remove encherto
                if (math.distance(old + Deltas[index].delta, Goal[index].SubGoal) <= 1f && Settings.experiment.WayPointOn)
                {
                    var w = AgentCalculations.RandomWayPoint();

                    Goal[index] = new AgentGoal
                    {
                        SubGoal = w

                    };
                }
            }
        }




        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            MoveCloudsJob moveJob = new MoveCloudsJob()
            {
                Positions = markersGroup.Position,
                Deltas = markersGroup.AgentStep,
                Goal = markersGroup.Goal
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

        public static float3 RandomWayPoint()
        {
            System.Random r = new System.Random(System.DateTime.UtcNow.Millisecond);
            int i = r.Next(0, Settings.experiment.WayPoints.Length);
            return Settings.experiment.WayPoints[i];
        }
    }

}