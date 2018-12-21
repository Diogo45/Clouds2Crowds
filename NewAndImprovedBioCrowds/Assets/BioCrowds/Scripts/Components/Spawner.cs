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

    [UpdateAfter(typeof(AgentSpawner))]
    public class SpawnAgentBarrier : BarrierSystem { }


    [UpdateAfter(typeof(MarkerSpawnSystem)), UpdateBefore(typeof(CellTagSystem))]
    public class AgentSpawner : JobComponentSystem
    {
        [Inject] public SpawnAgentBarrier barrier;

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

        public NativeList<Parameters> spawnList;
        public static EntityArchetype AgentArchetype;
        public static MeshInstanceRenderer AgentRenderer;


        public struct SpawnGroup : IJobParallelFor
        {

            [ReadOnly] public NativeArray<Parameters> spawnList;
            public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(int index)
            {
                int doNotFreeze = 0;

                float3 convertedOrigin = WindowManager.Clouds2Crowds(spawnList[index].spawnOrigin);
                float2 dim = spawnList[index].spawnDimensions;

                int qtdAgtTotal = spawnList[index].qtdAgents;
                int maxZ = (int)(convertedOrigin.z + dim.y);
                int maxX = (int)(convertedOrigin.x + dim.x);
                int minZ = (int)convertedOrigin.z;
                int minX = (int)convertedOrigin.x;
                float maxSpeed = spawnList[index].maxSpeed;

                //Debug.Log(" MAX MIN " + new int4(maxZ, minZ, maxX, minX));

                int startID = 0;

                for (int i = index - 1; i >= 0; i--)
                {
                    startID += spawnList[i].qtdAgents;
                }

                System.Random r = new System.Random(DateTime.UtcNow.Millisecond);

                int CellX = minX + 1;
                int CellZ = minZ + 1;
                int CellY = 0;
                //Debug.Log("CELL: " + CellX + " " + CellZ);

                //Problema total agents
                for (int i = startID; i < qtdAgtTotal + startID; i++)
                {

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







                    float3 g = spawnList[index].goal;

                    //x = UnityEngine.Random.Range(x - 0.99f, x + 0.99f);
                    //float y = 0f;
                    //z = UnityEngine.Random.Range(z - 0.99f, z + 0.99f);




                    CommandBuffer.CreateEntity(index, AgentArchetype);
                    CommandBuffer.SetComponent(index, new Position { Value = new float3(x, y, z) });
                    CommandBuffer.SetComponent(index, new Rotation { Value = Quaternion.identity });
                    Debug.Log(maxSpeed / Settings.instance.FramesPerSecond);
                    CommandBuffer.SetComponent(index, new AgentData
                    {
                        ID = i,
                        MaxSpeed = maxSpeed / Settings.instance.FramesPerSecond,
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


                    CommandBuffer.AddSharedComponent(index, AgentRenderer);



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
               ComponentType.Create<Counter>());


            spawnList = new NativeList<Parameters>(1, Allocator.Persistent);
            //spawnList[0] = new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 50, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(52, 52, 0) };
            spawnList.Add(new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 10, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(52, 52, 0) });

            spawnList.Add(new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 10, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(50, 50, 0) });


            spawnList.Add(new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 15, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(52, 50, 0) });

            spawnList.Add(new Parameters { cloud = 0, goal = new int3(50, 0, 25), maxSpeed = 1.3f, qtdAgents = 15, spawnDimensions = new int2(2, 2), spawnOrigin = new float3(50, 52, 0) });

        }

        protected override void OnStartRunning()
        {
            AgentRenderer = BioCrowdsBootStrap.GetLookFromPrototype("AgentRenderer");
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var SpawnGroupJob = new SpawnGroup
            {
                spawnList = spawnList,
                CommandBuffer = barrier.CreateCommandBuffer().ToConcurrent()
            };


            var SpawnGroupHandle = SpawnGroupJob.Schedule(spawnList.Length, Settings.BatchSize, inputDeps);

            SpawnGroupHandle.Complete();

            for (int i = 0; i < spawnList.Length; i++)
            {
                lastAgentId += spawnList[i].qtdAgents;
            }
            Settings.agentQuantity = lastAgentId;

            spawnList.Clear();
            return SpawnGroupHandle;
        }

        protected override void OnDestroyManager()
        {
            spawnList.Dispose();
        }


    }
}