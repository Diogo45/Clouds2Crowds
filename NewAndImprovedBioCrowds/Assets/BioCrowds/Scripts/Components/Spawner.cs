using UnityEngine;
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
        private bool _ChangedWindow;
        private void ChangedWindow(float3 newPosition, float2 newSize)
        {
            _ChangedWindow = true;
        }
        // Holds how many agents have been spawned up to the i-th cell.
        public NativeArray<int> AgentAtCellQuantity;


        [Inject] public SpawnAgentBarrier barrier;
        [Inject] public Clouds2CrowdsSystem clouds2Crowds;
        public NativeList<Parameters> parBuffer;


        public int lastAgentId;

        public struct Parameters
        {
            public int cloud;
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

        public struct InicialSpawn : IJobParallelFor
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

                    CommandBuffer.CreateEntity(index, AgentArchetype);
                    CommandBuffer.SetComponent(index, new Position { Value = new float3(x, y, z) });
                    CommandBuffer.SetComponent(index, new Rotation { Value = Quaternion.identity });
                    CommandBuffer.SetComponent(index, new AgentData
                    {
                        ID = i,
                        MaxSpeed = maxSpeed/Settings.experiment.FramesPerSecond,
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

                    CommandBuffer.SetComponent<BioCrowdsAnchor>(index, new BioCrowdsAnchor { Pivot = float3.zero });

                    CommandBuffer.AddSharedComponent(index, AgentRenderer);
                    CommandBuffer.AddSharedComponent(index, new AgentCloudID { CloudID = spawnList.cloud });


                }


            }
        }


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
                    return;
                }
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
                    //Debug.Log(maxSpeed / Settings.experiment.FramesPerSecond);
                    CommandBuffer.SetComponent(index, new AgentData
                    {
                        ID = i,
                        MaxSpeed = maxSpeed + (maxSpeed /* (float)(r.NextDouble() */ * 0.2f),// / Settings.experiment.FramesPerSecond,
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
               ComponentType.Create<Counter>(),
               ComponentType.Create<BioCrowdsAnchor>());

            


        }

        protected override void OnStopRunning()
        {
            AgentAtCellQuantity.Dispose();
            parBuffer.Dispose();
        }

        protected override void OnStartRunning()
        {
            AgentRenderer = BioCrowdsBootStrap.GetLookFromPrototype("AgentRenderer");
            UpdateInjectedComponentGroups();
            
            lastAgentId = 0;
            //If bioclouds isn't enabled we must use other job to spawn the agents
            //Here we get the necessary data from the experiment file
            if (!Settings.experiment.BioCloudsEnabled)
            {
                var exp = Settings.experiment.SpawnAreas;

                parBuffer = new NativeList<Parameters>(exp.Length, Allocator.Persistent);

                for (int i = 0; i < exp.Length; i++)
                {
                    Parameters par = new Parameters
                    {
                        cloud = i,
                        goal = exp[i].goal,
                        maxSpeed = exp[i].maxSpeed,
                        qtdAgents = exp[i].qtd,
                        spawnOrigin = exp[i].min,
                        spawnDimensions = new float2(exp[i].max.x, exp[i].max.z)
                    };

                    parBuffer.Add(par);
                }
                AgentAtCellQuantity = new NativeArray<int>(parBuffer.Length, Allocator.Persistent);

            }
            else
            {
                AgentAtCellQuantity = new NativeArray<int>(m_CellGroup.Length, Allocator.Persistent);

            }

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (Settings.experiment.BioCloudsEnabled)
            {
                lastAgentId = AgentAtCellQuantity[AgentAtCellQuantity.Length - 1] + lastAgentId;

                int lastValue = 0;

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

                return SpawnGroupHandle;
            }
            else
            {

                int lastValue = parBuffer[0].qtdAgents;
                AgentAtCellQuantity[0] = 0;
                for (int i = 1; i < parBuffer.Length; i++)
                {

                    AgentAtCellQuantity[i] = lastValue + AgentAtCellQuantity[i - 1];
                    Parameters spawnList = parBuffer[i-1];
                    lastValue = spawnList.qtdAgents;
                    
                }
                var job = new InicialSpawn
                {
                    AgentAtCellQuantity = AgentAtCellQuantity,
                    CommandBuffer = barrier.CreateCommandBuffer().ToConcurrent(),
                    parBuffer = parBuffer
                };

                var handle = job.Schedule(parBuffer.Length, Settings.BatchSize, inputDeps);
                handle.Complete();
                this.Enabled = false;
                return handle;



            }

}

    }


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
                    CommandBuffer.DestroyEntity(index, entities[index]);
                }
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