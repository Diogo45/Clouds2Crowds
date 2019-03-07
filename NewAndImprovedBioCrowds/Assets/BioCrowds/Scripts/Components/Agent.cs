
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
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

    public struct Active : IComponentData
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
                //Debug.Log(agent + " " + agentPos);
                AgentIDToPos.TryAdd(agent, agentPos);
                float distCell = math.distance((float3)MyCell[index].Value, agentPos);


                for (int i = startX; i <= endX; i = i + 2)
                {
                    for (int j = startZ; j <= endZ; j = j + 2)
                    {
                        int3 key = new int3(i, 0, j);

                        CellToAgent.Add(key, agent);
                        //Debug.Log(cell + " " + key);

                        float distNewCell = math.distance((float3)key, agentPos);
                        if (distNewCell < distCell)
                        {
                            distCell = distNewCell;
                            MyCell[index] = new CellName { Value = key };
                        }
                    }
                }

            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //int qtdAgts = Settings.agentQuantity;
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
            //TODO dynamize
            CellToMarkedAgents = new NativeMultiHashMap<int3, int>(160000, Allocator.Persistent);

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


    [UpdateInGroup(typeof(MovementVectorsSystemGroup)), UpdateAfter(typeof(MarkerWeightSystem)), UpdateAfter(typeof(BioCities.CellMarkSystem))]
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
        [Inject] MarkerSystem markerSystem;

        [Inject] MarkerWeightSystem totalWeightSystem;

        //TODO BioClouds stuff
        [Inject] BioCities.CellMarkSystem m_BioCloudsCellMarkSystem;
        [Inject] BioCities.CloudCellTagSystem m_BioCloudsCellTagSystem;
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

            //TODO BioClouds Data

            [ReadOnly] public NativeHashMap<int, BioCities.CloudIDPosRadius> BioClouds2PosMap;
            [ReadOnly] public NativeHashMap<int, int> BioCloudsCell2OwningCloudMap;


            //END BIOCLOUDS DATA

            public void Execute(int index)
            {

                BioCities.CloudIDPosRadius CloudPos;
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

                //float3 cloudauxinPosition = WindowManager.Crowds2Clouds(currentMarkerPosition);
                //int bioCloudsCellIdOfAuxin = GridConverter.Position2CellID(cloudauxinPosition);
                //int owningCloud;
                //bool cloudOwnsMarker = BioCloudsCell2OwningCloudMap.TryGetValue(bioCloudsCellIdOfAuxin, out owningCloud);
                
               // bool sameCloudAsMyself = false;
                //if (cloudOwnsMarker)
                //{
                //    sameCloudAsMyself = owningCloud == AgentCloudID[index].CloudID;
               // }

                //float extraWeight = 0f;
                //if (sameCloudAsMyself)
                //{
                //    extraWeight = 2f;
                //}


                float F = AgentCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, AgentGoals[index].SubGoal - AgentPos[index].Value);

                F += extraweight * 0.1f;

                direction += AgentCalculations.PartialW(totalW, F) * AgentData[index].MaxSpeed * (currentMarkerPosition - AgentPos[index].Value);



                while (AgentMarkersMap.TryGetNextValue(out currentMarkerPosition, ref it))
                {

                    //BioClouds
                    //cloudauxinPosition = WindowManager.Crowds2Clouds(currentMarkerPosition);
                    //bioCloudsCellIdOfAuxin = GridConverter.Position2CellID(cloudauxinPosition);
                    //cloudOwnsMarker = BioCloudsCell2OwningCloudMap.TryGetValue(bioCloudsCellIdOfAuxin, out owningCloud);

                    //sameCloudAsMyself = false;
                    //if (cloudOwnsMarker)
                    //{
                    //    sameCloudAsMyself = owningCloud == AgentCloudID[index].CloudID;
                    //}

                    //extraWeight = 0f;
                    //if (sameCloudAsMyself)
                    //{
                    //    extraWeight = 2f;
                    //}
                    //BIOCLOUDS

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

            var calculateMoveStepJob = new CalculateAgentMoveStep()
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

            //Debug.Log("AgentCount: " + agentGroup.Length);

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
                //Debug.Log("MOVE:" + Positions[index].Value);

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