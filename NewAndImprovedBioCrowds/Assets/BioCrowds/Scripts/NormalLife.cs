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
using Unity.Rendering;


namespace BioCrowds
{
   

    [System.Serializable]
    public struct Counter : IComponentData
    {
        public int Value;
    }

    [System.Serializable]
    public struct NormalLifeData : IComponentData
    {
        public float stress;
        public float confort;
        public float agtStrAcumulator;
        public float movStrAcumulator;
        public float incStress;
    }





    
    [UpdateInGroup(typeof(MarkerSystemGroup)), UpdateAfter(typeof(CellTagSystem))]
    public class NormalLifeMarkerSystem : JobComponentSystem
    {

        public static Settings BioSettings;
        //public static NormalLifeSettings NLSettings;

        [Inject] public CellTagSystem CellTagSystem;

        public NativeMultiHashMap<int, float3> AgentMarkers;
        public NativeHashMap<int, float3> AgentToSubGoal;
        //TODO: Change this to input stress in every system
        public NativeHashMap<int, float> AgentStress;

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Positions;
            [ReadOnly] public ComponentDataArray<CellName> MyCell;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public ComponentDataArray<NormalLifeData> NormalLifeData;

            public ComponentDataArray<Counter> Counter;
            [ReadOnly] public readonly int Length;

        }
        [Inject] AgentGroup agentGroup;

        public struct MarkerGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<CellName> MarkerCell;
            public ComponentDataArray<MarkerData> MarkerData;
            [ReadOnly] public readonly int Length;
        }
        [Inject] MarkerGroup markerGroup;

        struct StressHashMap : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<NormalLifeData> NormalLifeData;
            [WriteOnly] public NativeHashMap<int, float>.Concurrent AgentStress;

            public void Execute(int index)
            {
                int thisAgent = AgentData[index].ID;
                float thisStress = NormalLifeData[index].stress;
                AgentStress.TryAdd(thisAgent, thisStress);
            }
        }

        struct SetGoals : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [WriteOnly] public NativeHashMap<int, float3>.Concurrent AgentToSubGoal;



            public void Execute(int index)
            {
                float3 subGoal = AgentGoal[index].SubGoal;
                int agtID = AgentData[index].ID;

                AgentToSubGoal.TryAdd(agtID, subGoal);


            }
        }

        struct TakeMarkers : IJobParallelFor
        {
            [WriteOnly] public NativeMultiHashMap<int, float3>.Concurrent AgentMarkers;
            [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;
            [ReadOnly] public NativeMultiHashMap<int3, int> cellToAgent;
            [ReadOnly] public NativeHashMap<int, float> AgentStress;

            public ComponentDataArray<Counter> Counter;
            public ComponentDataArray<MarkerData> MarkerData;
            [ReadOnly] public ComponentDataArray<CellName> MarkerCell;
            [ReadOnly] public ComponentDataArray<Position> MarkerPos;
            [ReadOnly] public NativeHashMap<int, float3> AgentToSubGoal;
            [ReadOnly] public ComponentDataArray<NormalLifeData> NormalLifeData;


            public void Execute(int index)
            {
                NativeMultiHashMapIterator<int3> iter;

                //int3 currentCell;
                int currentAgent = -1;
                int bestAgent = -1;
                float agentRadius = 1f;
                float closestDistance = agentRadius + 1;
                float currentTotalFactor = float.PositiveInfinity;
                float bestTotalFactor = float.PositiveInfinity;


                bool keepgoing = cellToAgent.TryGetFirstValue(MarkerCell[index].Value, out currentAgent, out iter);


                if (!keepgoing) return;



                float3 agentPos;
                AgentIDToPos.TryGetValue(currentAgent, out agentPos);

                float dist = math.distance(MarkerPos[index].Value, agentPos);

                float3 distVec = MarkerPos[index].Value - agentPos;

                float3 goal;
                AgentToSubGoal.TryGetValue(currentAgent, out goal);

                float dotProd = distVec.x * goal.x + distVec.y * goal.y + distVec.z * goal.z;

                float dirFact = 2f - (1f + (dotProd / (math.distance(float3.zero, goal) * dist))) / 2f;

                float stress;
                AgentStress.TryGetValue(currentAgent, out stress);

                float x1 = (float) math.sin(stress * math.PI / 2f);

                currentTotalFactor = dist * (x1 * dirFact + (1 - x1));


                if (dist < agentRadius && currentTotalFactor < bestTotalFactor)
                {
                    bestTotalFactor = currentTotalFactor;
                    bestAgent = currentAgent;
                }

                while (cellToAgent.TryGetNextValue(out currentAgent, ref iter))
                {
                    AgentIDToPos.TryGetValue(currentAgent, out agentPos);
                    dist = math.distance(MarkerPos[index].Value, agentPos);
                    distVec = MarkerPos[index].Value - agentPos;
                    AgentToSubGoal.TryGetValue(currentAgent, out goal);
                    dotProd = distVec.x * goal.x + distVec.y * goal.y + distVec.z * goal.z;
                    dirFact = 2f - (1f + (dotProd / (math.distance(float3.zero, goal) * dist))) / 2f;
                    AgentStress.TryGetValue(currentAgent, out stress);
                    x1 = (float)math.sin(stress * math.PI / 2f);
                    currentTotalFactor = dist * (x1 * dirFact + (1 - x1));


                    if (dist < agentRadius && currentTotalFactor <= bestTotalFactor)
                    {
                        if (currentTotalFactor != bestTotalFactor)
                        {
                            bestTotalFactor = currentTotalFactor;
                            bestAgent = currentAgent;
                        }
                        else
                        {
                            if (bestAgent > currentAgent)
                            {
                                bestAgent = currentAgent;
                                bestTotalFactor = currentTotalFactor;
                            }
                        }
                    }


                }

                if (bestAgent == -1) return;
                //Debug.Log(bestAgent);
                int myId = MarkerData[index].id;
                MarkerData[index] = new MarkerData() { id = myId, agtID = bestAgent };
                AgentMarkers.Add(bestAgent, MarkerPos[index].Value);

                
                
            }

        }





        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            
            AgentMarkers.Clear();
            AgentStress.Clear();
            AgentToSubGoal.Clear();

            var SetGoals = new SetGoals
            {
                AgentToSubGoal = AgentToSubGoal.ToConcurrent(),
                AgentData = agentGroup.AgentData,
                AgentGoal = agentGroup.AgentGoal
            };

            var setGoalsHandle = SetGoals.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

            setGoalsHandle.Complete();


            var SetStress = new StressHashMap
            {
                AgentData = agentGroup.AgentData,
                NormalLifeData = agentGroup.NormalLifeData,
                AgentStress = AgentStress.ToConcurrent()
            };

            var SetStressHandle = SetStress.Schedule(agentGroup.Length, Settings.BatchSize, setGoalsHandle);

            SetStressHandle.Complete();

            TakeMarkers takeMarkersJob = new TakeMarkers
            {
                AgentIDToPos = CellTagSystem.AgentIDToPos,
                AgentMarkers = AgentMarkers.ToConcurrent(),
                cellToAgent = CellTagSystem.CellToMarkedAgents,
                MarkerCell = markerGroup.MarkerCell,
                MarkerPos = markerGroup.Position,
                MarkerData = markerGroup.MarkerData,
                Counter = agentGroup.Counter,
                AgentStress = AgentStress,
                AgentToSubGoal = AgentToSubGoal,
                NormalLifeData = agentGroup.NormalLifeData
               
            };

            int qtdAgents = Settings.agentQuantity;
            var takeMakersHandle = takeMarkersJob.Schedule(markerGroup.Length, qtdAgents / 4, inputDeps);
            takeMakersHandle.Complete();


            NativeMultiHashMap<int, float3> agtM = AgentMarkers;

            NativeMultiHashMapIterator<int> iter;
            float3 marker;
            for (int i = 0; i < agentGroup.Length; i++)
            {
                bool keepGoing = agtM.TryGetFirstValue(agentGroup.AgentData[i].ID, out marker, out iter);
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
        }


        protected override void OnStartRunning()
        {
            int qtdAgents = Settings.agentQuantity;
            float densityToQtd = Settings.experiment.MarkerDensity / Mathf.Pow(Settings.experiment.markerRadius, 2f);
            int qtdMarkers = Mathf.FloorToInt(densityToQtd);

            AgentMarkers = new NativeMultiHashMap<int, float3>(qtdAgents * qtdMarkers * 4, Allocator.Persistent);
            AgentToSubGoal = new NativeHashMap<int, float3>(qtdAgents * 2, Allocator.Persistent);
            AgentStress = new NativeHashMap<int, float>(qtdAgents * 2, Allocator.Persistent);
        }

        protected override void OnStopRunning()
        {
            AgentMarkers.Dispose();
            AgentToSubGoal.Dispose();
            AgentStress.Dispose();
        }

    }

    [UpdateBefore(typeof(NormaLifeAgentMovementVectors)), UpdateAfter(typeof(NormalLifeMarkerSystem))]
    public class MarkerCounter : JobComponentSystem
    {
        [Inject] NormalLifeMarkerSystem normalLifeMarkerSystem;

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            public ComponentDataArray<Counter> Counter;
            [ReadOnly] public readonly int Length;
        }
        [Inject] AgentGroup agentGroup;


        public struct ContaEssaBosta : IJobParallelFor
        {
            [ReadOnly] public NativeMultiHashMap<int, float3> AgentMarkers;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [WriteOnly] public ComponentDataArray<Counter> Counter;


            public void Execute(int index)
            {
                NativeMultiHashMapIterator<int> it;

                float3 nop;

               

                bool keepgoing = AgentMarkers.TryGetFirstValue(AgentData[index].ID, out nop, out it);

                if (!keepgoing)
                    return;

                int totalCount = 1;

                while (AgentMarkers.TryGetNextValue(out nop, ref it))
                {
                    totalCount++;
                }

                Counter[index] = new Counter { Value = totalCount };

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            ContaEssaBosta contaEssaBosta = new ContaEssaBosta
            {
                Counter = agentGroup.Counter,
                AgentData = agentGroup.AgentData,
                AgentMarkers = normalLifeMarkerSystem.AgentMarkers
            };

            var contaIsso = contaEssaBosta.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

            contaIsso.Complete();

            return contaIsso;
        }

    }

    [UpdateInGroup(typeof(MovementVectorsSystemGroup)), UpdateAfter(typeof(MarkerWeightSystem))]
    public class NormaLifeAgentMovementVectors : JobComponentSystem
    {
        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [WriteOnly] public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public ComponentDataArray<Counter> Counter;
            [ReadOnly] public readonly int Length;
        }


        [Inject] AgentGroup agentGroup;
        [Inject] NormalLifeMarkerSystem markerSystem;
        [Inject] MarkerWeightSystem totalWeightSystem;

        struct CalculateAgentMoveStep : IJobParallelFor
        {

            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoals;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public NativeMultiHashMap<int, float3> AgentMarkersMap;
            [ReadOnly] public NativeHashMap<int, float> AgentTotalW;
            [WriteOnly] public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public ComponentDataArray<Counter> Counter;


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

                int totalMarkers = Counter[index].Value;


                float F = AgentNormalLifeCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, AgentGoals[index].SubGoal - AgentPos[index].Value, totalMarkers);
                direction += AgentNormalLifeCalculations.PartialW(totalW, F) * AgentData[index].MaxSpeed * (currentMarkerPosition - AgentPos[index].Value);

                while (AgentMarkersMap.TryGetNextValue(out currentMarkerPosition, ref it))
                {
                    F = AgentNormalLifeCalculations.GetF(currentMarkerPosition, AgentPos[index].Value, AgentGoals[index].SubGoal - AgentPos[index].Value, totalMarkers);
                    direction += AgentNormalLifeCalculations.PartialW(totalW, F) * AgentData[index].MaxSpeed * (currentMarkerPosition - AgentPos[index].Value);
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
                Counter = agentGroup.Counter
            };

            var calculateMoveStepDeps = calculateMoveStepJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

            calculateMoveStepDeps.Complete();

            return calculateMoveStepDeps;
        }

    }

    
    [UpdateBefore(typeof(AgentMovementSystem))]//DONOW: ADD to update group before move system
    public class StressSystem : JobComponentSystem
    {
        [Inject] CellTagSystem cellTagSystem;
        public NativeHashMap<int, int> SurroundingAgents;


        public struct AgentGroup
        {
            [ReadOnly] public readonly int Length;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<CellName> MyCell;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoal;
            [ReadOnly] public EntityArray entities;
            [ReadOnly] public SharedComponentDataArray<MeshInstanceRenderer> AgentRenderer;
            public ComponentDataArray<AgentStep> AgentStep;
            public ComponentDataArray<NormalLifeData> NormalLifeData;
        }
        [Inject] AgentGroup agentGroup;


        struct FindSurroundingAgents : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<CellName> MyCell;
            [ReadOnly] public NativeMultiHashMap<int3, int> cellToAgent;
            [ReadOnly] public NativeHashMap<int, float3> AgentIDToPos;
            [WriteOnly] public NativeHashMap<int, int>.Concurrent SurroundingAgents;

            public void Execute(int index)
            {
                NativeMultiHashMapIterator<int3> iter;

                float3 myPos;
                int thisAgent = AgentData[index].ID;
                AgentIDToPos.TryGetValue(thisAgent, out myPos);

                int surroundingAgents = 0;

                int currentAgent = -1;
                float radius = 0.45f;

                //TODO:calculate cell
                bool keepgoing = cellToAgent.TryGetFirstValue(MyCell[index].Value, out currentAgent, out iter);

                if (!keepgoing) return;

                float3 agentPos;
                AgentIDToPos.TryGetValue(currentAgent, out agentPos);
                
                if(math.distance(agentPos, myPos) < radius)
                {
                    surroundingAgents++;
                }

                while(cellToAgent.TryGetNextValue(out currentAgent, ref iter))
                {
                    AgentIDToPos.TryGetValue(currentAgent, out agentPos);
                    if (math.distance(agentPos, myPos) < radius)
                    {
                        surroundingAgents++;
                    }
                }

                //Debug.Log(thisAgent + " " + surroundingAgents);
                SurroundingAgents.TryAdd(thisAgent, surroundingAgents);

            }
        }

        struct SetStress : IJobParallelFor
        {

            [ReadOnly] public NativeHashMap<int, int> SurroundingAgents;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentStep> AgentStep;
            public ComponentDataArray<NormalLifeData> NormalLifeData;


            

            public void Execute(int index)
            {
                int thisAgent = AgentData[index].ID;
                float movStrTime = NormalLifeData[index].movStrAcumulator;
                float agStrTime = NormalLifeData[index].agtStrAcumulator;
                float thisConfort = NormalLifeData[index].confort;
                float stress = NormalLifeData[index].stress;
                float speed = math.distance(float3.zero, AgentStep[index].delta);
                float prevIncrStr = NormalLifeData[index].incStress;

                int agentsInRange = 0;
                SurroundingAgents.TryGetValue(thisAgent, out agentsInRange);


                float timeConst = 0.03f;
                if (agentsInRange == 0)
                {
                    agStrTime -= 2 * agentsInRange;
                }
                else
                {
                    agStrTime += agentsInRange * timeConst;
                }

                float mov = speed / AgentData[index].MaxSpeed;

                if(mov > 0.2f)
                {
                    movStrTime = 2 * mov;
                }
                else
                {
                    movStrTime += mov * timeConst;
                }

                float theta = NormalLifeSettings.instance.STRESS_BETA * (agentsInRange + agStrTime / NormalLifeSettings.instance.STRESS_K1);
                float gamma = NormalLifeSettings.instance.STRESS_RHO * (mov + movStrTime / NormalLifeSettings.instance.STRESS_K1);

                float newIncrStr = theta + gamma;


                float newStress = newIncrStr;//stress + ( newIncrStr -  prevIncrStr) * (1 - stress);
               

                if (newStress < 0f)
                    newStress = 0f;
                else if (newStress > 1f)
                    newStress = 1f;
                NormalLifeData[index] = new NormalLifeData { agtStrAcumulator = agStrTime, confort = thisConfort, movStrAcumulator = movStrTime, stress = newStress, incStress = newIncrStr };


            }
        }

 

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SurroundingAgents.Clear();


            //TODO: Updata after pathfinding


           

            var FindSurroundingAgentsJob = new FindSurroundingAgents
            {
                SurroundingAgents = SurroundingAgents.ToConcurrent(),
                AgentData = agentGroup.AgentData,
                AgentIDToPos = cellTagSystem.AgentIDToPos,
                cellToAgent = cellTagSystem.CellToMarkedAgents,
                MyCell = agentGroup.MyCell
            };

            var FindSurroundingAgentsHandle = FindSurroundingAgentsJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

            FindSurroundingAgentsHandle.Complete();

            var SetStress = new SetStress
            {
                SurroundingAgents = SurroundingAgents,
                AgentData = agentGroup.AgentData,
                NormalLifeData = agentGroup.NormalLifeData,
                AgentStep = agentGroup.AgentStep
            };

            var SetStressHandle = SetStress.Schedule(agentGroup.Length, Settings.BatchSize, FindSurroundingAgentsHandle);

            SetStressHandle.Complete();





            //var entityManager = World.Active.GetOrCreateManager<EntityManager>();

            //for (int i = 0; i < agentGroup.Length; i++)
            //{

            //    var meshInstance = entityManager.GetSharedComponentData<MeshInstanceRenderer>(agentGroup.entities[i]);
                
            //    Material sub = new Material(meshInstance.material);
            //    float stress = agentGroup.NormalLifeData[i].stress;
            //    sub.color = new Color(stress, 0f, 1f - stress);
            //    var newinstance = new MeshInstanceRenderer { material = sub, mesh = meshInstance.mesh };

            //    entityManager.SetSharedComponentData(agentGroup.entities[i], newinstance);
            //    UpdateInjectedComponentGroups();
            //}

            return SetStressHandle;
        }

        protected override void OnStartRunning()
        {
            //DONOW: Change this to get from groups
            int qtdAgents = Settings.agentQuantity;
            SurroundingAgents = new NativeHashMap<int, int>(qtdAgents * 2, Allocator.Persistent);
        }

        protected override void OnStopRunning()
        {
            SurroundingAgents.Dispose();
        }


    }



    public static class AgentNormalLifeCalculations
    {
        //Current marker position, current cloud position and (goal position - cloud position) vector.
        public static float GetF(float3 markerPosition, float3 agentPosition, float3 agentGoalVector, float agentMarkerCount)
        {
            float Ymodule = math.length(markerPosition - agentPosition);

            float Xmodule = 1f;

            float dot = math.dot(markerPosition - agentPosition, math.normalize(agentGoalVector));

            if (Ymodule < 0.00001f)
                return 0.0f;

            float aux = ((1.0f / (1.0f + Ymodule)) * (1.0f + ((dot) / (Xmodule * Ymodule))));

            float n = (agentMarkerCount) / (NormalLifeSettings.instance.MAX_C_AUXINS);
            if (n > 1) n = 1;

            //quando o conforto diminui, a direção do vetor goal passa a ter menor efeito. quando o conforto é minimo, todos os vetores dos marcadores tem mesmo peso.
            //float retorno =  aux * Mathf.Sin(n * Mathf.PI/2f) + Mathf.Cos(n * Mathf.PI/2f);

            float peso = Mathf.Sin(n * Mathf.PI / 2f);
            return aux * peso + (1 - peso);

        }

        public static float PartialW(float totalW, float fValue)
        {
            return fValue / totalW;
        }

        
        
    }
    
}