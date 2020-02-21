﻿using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Rendering;
using System;

namespace BioCrowds
{
    [UpdateBefore(typeof(CellTagSystem))]
    public class SpawnerGroup { }


    /// <summary>
    /// Just a sinc point for the creation and modification of entities to be executed in the main thread
    /// </summary>
    [UpdateAfter(typeof(AgentSpawner)), UpdateInGroup(typeof(SpawnerGroup)), UpdateBefore(typeof(CellTagSystem))]
    public class SpawnAgentBarrier : BarrierSystem { }

    /// <summary>
    /// Spawns agents in runtime when BioClouds is active, instantiation occurs cell by cell where each one corresponds to a BioClouds cell. The data received is in the BioClouds, that is, the positions are (x,y,0) while BioCrowds is (x,0,z). 
    /// If bioclouds is not enabled for this experiment them we spawn only once with the data comming from the experiment file already in the necessary format.
    /// </summary>
    [UpdateAfter(typeof(MarkerSpawnSystem)), UpdateInGroup(typeof(SpawnerGroup)), UpdateBefore(typeof(CellTagSystem))]
    public class AgentSpawner : JobComponentSystem
    {

        // Holds how many agents have been spawned up to the i-th cell.
        public NativeArray<int> AgentAtGroupQuantity;


        [Inject] public SpawnAgentBarrier barrier;

        public NativeList<Parameters> parBuffer;


        public int lastAgentId;

        public struct Parameters
        {

            public int qtdAgents;
            public float3 spawnOrigin;
            public float2 spawnDimensions;
            public float maxSpeed;
            public float3 goal;

        }

        public static EntityArchetype AgentArchetype;
        public static MeshInstanceRenderer AgentRenderer;
        protected EntityCommandBuffer command_buffer;

        public struct CellGroup
        {
            [ReadOnly] public ComponentDataArray<BioCrowds.CellName> CellName;
            [ReadOnly] public SubtractiveComponent<BioCrowds.AgentData> Agent;
            [ReadOnly] public SubtractiveComponent<BioCrowds.MarkerData> Marker;

            [ReadOnly] public readonly int Length;
        }
        [Inject] public CellGroup m_CellGroup;

        public struct InitialSpawnStructured : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> AgentAtCellQuantity;
            [ReadOnly] public int LastIDUsed;

            [ReadOnly] public NativeList<Parameters> parBuffer;
            public EntityCommandBuffer.Concurrent CommandBuffer;


            public void Execute(int index)
            {
                int doNotFreeze = 0;

                var spawnList = parBuffer[index];
                float3 origin = spawnList.spawnOrigin;
                float2 dim = spawnList.spawnDimensions;

                int qtdAgtTotal = spawnList.qtdAgents;
                int maxZ = (int)(origin.z + dim.y);
                int maxX = (int)(origin.x + dim.x);
                int minZ = (int)origin.z;
                int minX = (int)origin.x;
                float maxSpeed = spawnList.maxSpeed;

                int startID = AgentAtCellQuantity[index] + LastIDUsed;

                int CellX = minX + 1;
                int CellZ = minZ + 1;
                int CellY = 0;

                int seed = DateTime.UtcNow.Millisecond;
                System.Random r = new System.Random(seed);

                //Debug.Log(spawnList.goal);

                float x = minX;
                float z = minZ;

                for (int i = startID; i < qtdAgtTotal + startID; i++)
                {

                    if (doNotFreeze > qtdAgtTotal)
                    {
                        doNotFreeze = 0;

                    }

                    
                    
                    float y = 0;

                    float3 g = spawnList.goal;
                    //Debug.Log(i);

                    CommandBuffer.CreateEntity(index, AgentArchetype);
                    CommandBuffer.SetComponent(index, new Position { Value = new float3(x, y, z) });
                    CommandBuffer.SetComponent(index, new Rotation { Value = Quaternion.identity });
                    CommandBuffer.SetComponent(index, new AgentData
                    {
                        ID = i,
                        MaxSpeed = maxSpeed / Settings.experiment.FramesPerSecond,
                        Radius = 1f
                    });
                    CommandBuffer.SetComponent(index, new AgentStep
                    {
                        delta = float3.zero
                    });
                    CommandBuffer.SetComponent(index, new AgentGoal { SubGoal = g, EndGoal = g });
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

                    if (x < maxX)
                    {
                        x += 5f;
                    }
                    else
                    {
                        if (z < maxZ)
                        {
                            x = minX;
                            z++;
                        }
                    }


                }


            }
        }


        public struct InitialSpawn : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> AgentAtCellQuantity;
            [ReadOnly] public int LastIDUsed;

            [ReadOnly] public NativeList<Parameters> parBuffer;
            public EntityCommandBuffer.Concurrent CommandBuffer;


            public void Execute(int index)
            {
                int doNotFreeze = 0;

                var spawnList = parBuffer[index];
                float3 origin = spawnList.spawnOrigin;
                float2 dim = spawnList.spawnDimensions;

                int qtdAgtTotal = spawnList.qtdAgents;
                int maxZ = (int)(origin.z + dim.y);
                int maxX = (int)(origin.x + dim.x);
                int minZ = (int)origin.z;
                int minX = (int)origin.x;
                float maxSpeed = spawnList.maxSpeed;

                int startID = AgentAtCellQuantity[index] + LastIDUsed;

                int CellX = minX + 1;
                int CellZ = minZ + 1;
                int CellY = 0;

                System.Random r = new System.Random(DateTime.UtcNow.Millisecond);

                //Debug.Log(spawnList.goal);

                for (int i = startID; i < qtdAgtTotal + startID; i++)
                {

                    if (doNotFreeze > qtdAgtTotal)
                    {
                        doNotFreeze = 0;

                    }

                    float x = (float)r.NextDouble() * (maxX - minX) + minX;
                    float z = (float)r.NextDouble() * (maxZ - minZ) + minZ;
                    float y = 0;


                    float3 g = spawnList.goal;
                    //Debug.Log(i);

                    CommandBuffer.CreateEntity(index, AgentArchetype);
                    CommandBuffer.SetComponent(index, new Position { Value = new float3(x, y, z) });
                    CommandBuffer.SetComponent(index, new Rotation { Value = Quaternion.identity });
                    CommandBuffer.SetComponent(index, new AgentData
                    {
                        ID = i,
                        MaxSpeed = maxSpeed/ Settings.experiment.FramesPerSecond,
                        Radius = 1f
                    });
                    CommandBuffer.SetComponent(index, new AgentStep
                    {
                        delta = float3.zero
                    });
                    CommandBuffer.SetComponent(index, new AgentGoal { SubGoal = g, EndGoal = g });
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
            //Here we define the agent archetype by adding all the Components, that is, all the Agent's data. 
            //The respective Systems will act upon the Components added, if such Systems exist.
            //Also we have to add Components from the modules such as NormalLife so we can turn them on and off in runtime
            AgentArchetype = entityManager.CreateArchetype(
               ComponentType.Create<Position>(),
               ComponentType.Create<Rotation>(),
               ComponentType.Create<AgentData>(),
               ComponentType.Create<AgentStep>(),
               ComponentType.Create<AgentGoal>(),
               ComponentType.Create<NormalLifeData>(),
               ComponentType.Create<Counter>());




        }

        protected override void OnStopRunning()
        {
            AgentAtGroupQuantity.Dispose();
            parBuffer.Dispose();
        }

        protected override void OnStartRunning()
        {
            AgentRenderer = BioCrowdsBootStrap.GetLookFromPrototype("AgentRenderer");
            UpdateInjectedComponentGroups();

            lastAgentId = 0;

            var exp = Settings.experiment.SpawnAreas;

            parBuffer = new NativeList<Parameters>(exp.Length, Allocator.Persistent);

            for (int i = 0; i < exp.Length; i++)
            {
                Parameters par = new Parameters
                {
                    //cloud = i,
                    goal = exp[i].goal,
                    maxSpeed = exp[i].maxSpeed,
                    qtdAgents = exp[i].qtd,
                    spawnOrigin = exp[i].min,
                    spawnDimensions = new float2(exp[i].max.x, exp[i].max.z)
                };
                Settings.agentQuantity += exp[i].qtd;
                parBuffer.Add(par);
            }
            AgentAtGroupQuantity = new NativeArray<int>(parBuffer.Length, Allocator.Persistent);


        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            command_buffer = barrier.CreateCommandBuffer();


            int lastValue = parBuffer[0].qtdAgents;
            AgentAtGroupQuantity[0] = 0;
            for (int i = 1; i < parBuffer.Length; i++)
            {

                AgentAtGroupQuantity[i] = lastValue + AgentAtGroupQuantity[i - 1];
                Parameters spawnList = parBuffer[i - 1];
                lastValue = spawnList.qtdAgents;

            }

            JobHandle handle;

            if (Settings.SpawnAgentStructured)
            {
                var job = new InitialSpawnStructured
                {
                    AgentAtCellQuantity = AgentAtGroupQuantity,
                    CommandBuffer = command_buffer.ToConcurrent(),
                    parBuffer = parBuffer
                };

                handle = job.Schedule(parBuffer.Length, Settings.BatchSize, inputDeps);
                lastAgentId = AgentAtGroupQuantity[AgentAtGroupQuantity.Length - 1] + lastValue;
                handle.Complete();
               

            }
            else
            {
                var job = new InitialSpawn
                {
                    AgentAtCellQuantity = AgentAtGroupQuantity,
                    CommandBuffer = command_buffer.ToConcurrent(),
                    parBuffer = parBuffer
                };

                handle = job.Schedule(parBuffer.Length, Settings.BatchSize, inputDeps);
                lastAgentId = AgentAtGroupQuantity[AgentAtGroupQuantity.Length - 1] + lastValue;
                handle.Complete();


            }

            this.Enabled = false;
            return handle;




        }

    }


    


    [UpdateAfter(typeof(AgentDespawner))]
    public class DespawnAgentBarrier : BarrierSystem { }

    [DisableAutoCreation]
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
                //    float3 posCloudCoord = WindowManager.Crowds2Clouds(AgtPos[index].Value);

                //    if (WindowManager.CheckDestructZone(posCloudCoord))
                //    {
                //        CommandBuffer.DestroyEntity(index, entities[index]);
                //    }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            if (Settings.experiment.BioCloudsEnabled)
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
            }
            else
            {
                //TODO: Define other methods for despawn
                this.Enabled = false;
                return inputDeps;
            }



        }

    }



}