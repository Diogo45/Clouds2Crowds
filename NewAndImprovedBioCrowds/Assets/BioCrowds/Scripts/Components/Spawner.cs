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
using Unity.Rendering;
using System;

namespace BioCrowds
{
    [UpdateBefore(typeof(CellTagSystem))]
    public class SpawnerGroup { }

    [UpdateAfter(typeof(AgentSpawner)), UpdateInGroup(typeof(SpawnerGroup))]
    public class SpawnAgentBarrier : BarrierSystem { }


    [UpdateAfter(typeof(MarkerSpawnSystem)), UpdateInGroup(typeof(SpawnerGroup))]
    public class AgentSpawner : JobComponentSystem
    {
        // Holds how many agents have been spawned up to the i-th cell.
        public NativeArray<int> AgentAtCellQuantity;


        [Inject] public SpawnAgentBarrier barrier;
        [Inject] public Clouds2CrowdsSystem clouds2Crowds;


        public int lastAgentId;

        public struct Parameters
        {
            public int cloud;
            //World positions
            public int qtdAgents;
            public float3 spawnOrigin;
            public float2 spawnDimensions;
            public float maxSpeed;
            public float3 goal;

        }

        public static EntityArchetype AgentArchetype;
        public static MeshInstanceRenderer AgentRenderer;


        public struct CellGroup
        {
            [ReadOnly] public ComponentDataArray<BioCrowds.CellName> CellName;
            [ReadOnly] public SubtractiveComponent<BioCrowds.AgentData> Agent;
            [ReadOnly] public SubtractiveComponent<BioCrowds.MarkerData> Marker;

            [ReadOnly] public readonly int Length;
        }
        [Inject] public CellGroup m_CellGroup;

        public struct SpawnGroup : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> AgentAtCellQuantity;
            [ReadOnly] public NativeHashMap<int, Parameters> parBuffer;
            [ReadOnly] public ComponentDataArray<BioCrowds.CellName> CellName;
            [ReadOnly] public int LastIDUsed;
            public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(int index)
            {
                int doNotFreeze = 0;

                float3 cellPos = WindowManager.Crowds2Clouds(CellName[index].Value);
                int ind = GridConverter.Position2CellID(cellPos);

                Parameters spawnList;
                bool keepgoing = parBuffer.TryGetValue(ind, out spawnList);
                if(!keepgoing)
                {
                    //Debug.Log("AAAAAAA");
                    return;
                }
                //Debug.Log("PASSO");
                float3 convertedOrigin = WindowManager.Clouds2Crowds(spawnList.spawnOrigin);
                float2 dim = spawnList.spawnDimensions;

                int qtdAgtTotal = spawnList.qtdAgents;
                int maxZ = (int)(convertedOrigin.z + dim.y);
                int maxX = (int)(convertedOrigin.x + dim.x);
                int minZ = (int)convertedOrigin.z;
                int minX = (int)convertedOrigin.x;
                float maxSpeed = spawnList.maxSpeed;

                //Debug.Log(" MAX MIN " + new int4(maxZ, minZ, maxX, minX));

                int startID = AgentAtCellQuantity[index] + LastIDUsed;
                

                System.Random r = new System.Random(DateTime.UtcNow.Millisecond);

                int CellX = minX + 1;
                int CellZ = minZ + 1;
                int CellY = 0;
                //Debug.Log("ConvertedOrigin:" + convertedOrigin + "CELL: " + CellX + " " + CellZ);

                //Problema total agents
                for (int i = startID; i < qtdAgtTotal + startID; i++)
                {

                    //Debug.Log("Agent id : " + i);

                    if (doNotFreeze > qtdAgtTotal)
                    {
                        doNotFreeze = 0;
                        //maxZ += 2;
                        //maxX += 2;
                    }

                    float x = (float)r.NextDouble() * (maxX - minX) + minX;
                    float z = (float)r.NextDouble() * (maxZ - minZ) + minZ;
                    float y = 0;
                    //Debug.Log("AGENT: " + x + " " + z);

                    


                    float3 g = WindowManager.Clouds2Crowds(spawnList.goal);

                    //x = UnityEngine.Random.Range(x - 0.99f, x + 0.99f);
                    //float y = 0f;
                    //z = UnityEngine.Random.Range(z - 0.99f, z + 0.99f);




                    CommandBuffer.CreateEntity(index, AgentArchetype);
                    CommandBuffer.SetComponent(index, new Position { Value = new float3(x, y, z) });
                    CommandBuffer.SetComponent(index, new Rotation { Value = Quaternion.identity });
                    //Debug.Log(maxSpeed / Settings.instance.FramesPerSecond);
                    CommandBuffer.SetComponent(index, new AgentData
                    {
                        ID = i,
                        MaxSpeed = maxSpeed + (maxSpeed * (float)(r.NextDouble() * 0.2f)-0.1f),// / Settings.instance.FramesPerSecond,
                        Radius = 1f
                    });
                    CommandBuffer.SetComponent(index, new AgentStep
                    {
                        delta = float3.zero
                    });
                    CommandBuffer.SetComponent(index, new Rotation
                    {
                        Value = quaternion.identity
                    });
                    CommandBuffer.SetComponent(index, new CellName { Value = new int3(CellX, CellY, CellZ) });
                    CommandBuffer.SetComponent(index, new AgentGoal { SubGoal = g, EndGoal = g });
                    //entityManager.AddComponent(newAgent, ComponentType.FixedArray(typeof(int), qtdMarkers));
                    //TODO:Normal Life stuff change
                    CommandBuffer.SetComponent(index, new Counter { Value = 0 });
                    CommandBuffer.SetComponent(index, new NormalLifeData
                    {
                        confort = 0,
                        stress = 0,
                        agtStrAcumulator = 0f,
                        movStrAcumulator = 0f,
                        incStress = 0f
                    });

                    CommandBuffer.SetComponent<BioCrowdsAnchor>(index, new BioCrowdsAnchor { Pivot = WindowManager.instance.originBase });

                    CommandBuffer.AddSharedComponent(index, AgentRenderer);
                    CommandBuffer.AddSharedComponent(index, new AgentCloudID { CloudID = spawnList.cloud });


                }
            }
        }


        protected override void OnCreateManager()
        {
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();
            AgentArchetype = entityManager.CreateArchetype(
               ComponentType.Create<Position>(),
               ComponentType.Create<Rotation>(),
               ComponentType.Create<CellName>(),
               ComponentType.Create<AgentData>(),
               ComponentType.Create<AgentStep>(),
               ComponentType.Create<AgentGoal>(),
               ComponentType.Create<NormalLifeData>(),
               ComponentType.Create<Animator>(),
               ComponentType.Create<Counter>(),
               ComponentType.Create<BioCrowdsAnchor>());//,
               //ComponentType.Create<Attach>());


            //spawnList[0] = new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 50, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(52, 52, 0) };
            //spawnList.Add(new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 10, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(52, 52, 0) });

            //spawnList.Add(new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 10, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(50, 50, 0) });


            //spawnList.Add(new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 15, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(52, 50, 0) });

            //spawnList.Add(new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 15, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(50, 52, 0) });

        }

        protected override void OnStartRunning()
        {
            AgentRenderer = BioCrowdsBootStrap.GetLookFromPrototype("AgentRenderer");
            UpdateInjectedComponentGroups();
            AgentAtCellQuantity = new NativeArray<int>(m_CellGroup.Length, Allocator.Persistent);
            lastAgentId = 0;

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            //Debug.Log("agents spawned " + lastAgentId);
            lastAgentId = AgentAtCellQuantity[AgentAtCellQuantity.Length - 1] + lastAgentId;

            //Debug.Log("L2 " + clouds2Crowds.parameterBuffer.Length);
            int lastValue = 0;

            //TODO make parallel
            for (int i = 1; i < m_CellGroup.Length; i++)
            {
                float3 cellPos = WindowManager.Crowds2Clouds(m_CellGroup.CellName[i].Value);
                int ind = GridConverter.Position2CellID(cellPos);
                
                AgentAtCellQuantity[i] = lastValue + AgentAtCellQuantity[i - 1];
                if (clouds2Crowds.parameterBuffer.TryGetValue(ind, out Parameters spawnList))
                {
                    lastValue = spawnList.qtdAgents;
                }
            }


            var SpawnGroupJob = new SpawnGroup
            {
                parBuffer = clouds2Crowds.parameterBuffer,
                CommandBuffer = barrier.CreateCommandBuffer().ToConcurrent(),
                CellName = m_CellGroup.CellName,
                AgentAtCellQuantity = AgentAtCellQuantity,
                LastIDUsed = lastAgentId
            };

            

            var SpawnGroupHandle = SpawnGroupJob.Schedule(m_CellGroup.Length, Settings.BatchSize, inputDeps);

            

            SpawnGroupHandle.Complete();

            //for (int i = 0; i < clouds2Crowds.parameterBuffer.Length; i++)
            //{
            //    Parameters par;
            //    clouds2Crowds.parameterBuffer.TryGetValue(i, out par);
            //}

            //spawnList.Clear();

            return SpawnGroupHandle;
        }

    }

    //[UpdateAfter(typeof(SpawnAgentBarrier)), UpdateInGroup(typeof(SpawnerGroup))]
    //public class SpawnAttacherSystem : ComponentSystem
    //{
    //    public struct AgentGroup
    //    {
    //        [ReadOnly] public ComponentDataArray<Position> AgtPos;
    //        [ReadOnly] public ComponentDataArray<AgentData> AgtData;
    //        [ReadOnly] public SubtractiveComponent<Attached> Attached;
    //        [ReadOnly] public EntityArray entities;
    //        [ReadOnly] public readonly int Length;
    //    }
    //    [Inject] AgentGroup m_agentGroup;

    //    //public Entity Window;

    //    protected override void OnUpdate()
    //    {
    //        EntityManager entityManager =  World.Active.GetOrCreateManager<EntityManager>();

    //        for(int i = 0; i < m_agentGroup.Length; i++)
    //        {
    //            entityManager.SetComponentData(m_agentGroup.entities[i], new Attach
    //            {
    //                Child = m_agentGroup.entities[i],
    //                Parent = WindowManager.Window
    //            });
    //            Debug.Log("Attached");
    //        }
    //    }
    //}

    //[UpdateAfter(typeof(SpawnAgentBarrier)), UpdateInGroup(typeof(SpawnerGroup))]
    //public class SpawnAttacherSystem : ComponentSystem
    //{
    //    public struct AgentGroup
    //    {
    //        [ReadOnly] public ComponentDataArray<Position> AgtPos;
    //        [ReadOnly] public ComponentDataArray<AgentData> AgtData;
    //        [ReadOnly] public ComponentDataArray<LocalToWorld> WindowTransform;
    //        [ReadOnly] public EntityArray entities;
    //        [ReadOnly] public readonly int Length;
    //    }
    //    [Inject] AgentGroup m_agentGroup;

    //    //public Entity Window;

    //    protected override void OnUpdate()
    //    {
    //        EntityManager entityManager = World.Active.GetOrCreateManager<EntityManager>();

    //        for (int i = 0; i < m_agentGroup.Length; i++)
    //        {

    //            //Debug.Log(m_agentGroup.WindowTransform[i].Value);
    //        }
    //    }
    //}

    [UpdateAfter(typeof(AgentDespawner))]
    public class DespawnAgentBarrier : BarrierSystem { }

    [UpdateAfter(typeof(AgentMovementSystem))]
    public class AgentDespawner : JobComponentSystem
    {

        [Inject] DespawnAgentBarrier barrier;

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgtPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgtData;
            [ReadOnly] public EntityArray entities;
            [ReadOnly] public readonly int Length;

        }
        [Inject] AgentGroup agentGroup;
        

        public struct CheckAreas : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<Position> AgtPos;
            [ReadOnly] public EntityArray entities;
            public EntityCommandBuffer.Concurrent CommandBuffer;


            public void Execute(int index)
            {
                float3 posCloudCoord = WindowManager.Crowds2Clouds(AgtPos[index].Value);
                
                if (WindowManager.CheckDestructZone(posCloudCoord))
                {
                    //Debug.Log(posCloudCoord);
                    CommandBuffer.DestroyEntity(index, entities[index]);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var CheckArea = new CheckAreas
            {
                AgtPos = agentGroup.AgtPos,
                CommandBuffer = barrier.CreateCommandBuffer().ToConcurrent(),
                entities = agentGroup.entities
            };

            var CheckAreaHandle = CheckArea.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);
            CheckAreaHandle.Complete();
            return CheckAreaHandle;
            //return inputDeps;
            
        }

    }



}