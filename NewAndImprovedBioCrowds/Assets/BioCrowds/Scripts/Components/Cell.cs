using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using System;

namespace BioCrowds
{
    public struct CellName : IComponentData
    {
        
        public int3 Value;

    }

    public struct MarkerCellName: ISharedComponentData
    {
        public int3 Value;

    }

    public struct SpawnData : IComponentData
    {
        
        public int qtdPerCell;
    }





    [UpdateBefore(typeof(CellTagSystem)), UpdateAfter(typeof(MarkerSpawnSystem))]
    public class SpawnBarrier : BarrierSystem { }

    /// <summary>
    /// Spawns markers cell by cell with random positions and a marker radius apart, given the marker density
    /// </summary>
   
    
    [UpdateBefore(typeof(CellTagSystem))]
    public class MarkerSpawnSystem : JobComponentSystem
    {
        [Inject] private SpawnBarrier m_SpawnerBarrier;
        public static MeshInstanceRenderer MarkerRenderer;

        public static NativeMultiHashMap<int3, int> CellMarkers;
        public NativeArray<int> Seeds;

        public static EntityArchetype MakerArchetype;

        public struct SpawnParameters
        {
            [ReadOnly] public ComponentDataArray<SpawnData> SpawnData;
        }
        [Inject] SpawnParameters spawnParameters;

        public struct CellData
        {
            [ReadOnly] public ComponentDataArray<Position> CellPos;
            [ReadOnly] public ComponentDataArray<CellName> CellName;
            [ReadOnly] public readonly int Length;
            [ReadOnly] public SubtractiveComponent<AgentData> Subtractive;

        }
        [Inject] CellData cellData;

        public MarkerSpawnSystem()
        {
            this.Enabled = true;
        }




        

        struct SpawnMarkers : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<Position> CellPos;
            [ReadOnly] public ComponentDataArray<CellName> cellNames;
            public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ComponentDataArray<SpawnData> SpawnData;
            //[ReadOnly] public NativeArray<int> Seeds;

            bool IsInside(Vector3 t_pos, Vector3 t_scale, Vector3 pos)
            {
                float minx, maxx, minz, maxz;
                minx = t_pos.x - t_scale.x / 2.0f;
                maxx = t_pos.x + t_scale.x / 2.0f;
                minz = t_pos.z - t_scale.z / 2.0f;
                maxz = t_pos.z + t_scale.z / 2.0f;

                return (pos.x >= minx && pos.x <= maxx) && (pos.z >= minz && pos.z <= maxz);
            }

            public void Execute(int index)
            {
                int qtdMarkers = SpawnData[0].qtdPerCell;
                int flag = 0;
                int markersAdded = 0;
                NativeList<Position> tempCellMarkers = new NativeList<Position>(qtdMarkers, Allocator.Persistent);

                System.Random r = new System.Random(DateTime.UtcNow.Millisecond);
                //System.Random r = new System.Random(Seeds[index]);

                for (int i = 0; i < qtdMarkers; i++)
                {

                    //FUTURE: Add a proper 'y' coordinate
                    float x = ((float)r.NextDouble()*2f - 1f) + cellNames[index].Value.x;
                    float y = 0f;
                    float z = ((float)r.NextDouble()*2f - 1f) + cellNames[index].Value.z;

                    var cell_pos = new Vector3(x, y, z);




                    bool canInstantiate = true;
                    bool is_inside = false;

                    foreach (CrowdExperiment.ObstacleArea obstacle in CrowdExperiment.instance.obstacleAreas)
                    {
                        if (IsInside(obstacle.start, obstacle.end, cell_pos))
                        {
                            is_inside = true;
                        }

                    }

                    for (int j = 0; j < tempCellMarkers.Length; j++)
                    {
                        float distanceAA = math.distance(new float3(x, y, z), tempCellMarkers[j].Value);
                        if (distanceAA < CrowdExperiment.instance.markerRadius)
                        {
                            canInstantiate = false;
                            break;
                        }
                    }

                    
                   
                    if (canInstantiate && !is_inside)
                    {
                        CommandBuffer.CreateEntity(index, MakerArchetype);

                        CommandBuffer.SetComponent(index, new Position
                        {
                            Value = new float3(x, y, z)
                        });
                        CommandBuffer.SetComponent(index, new MarkerData
                        {
                            id = markersAdded
                        });
                        CommandBuffer.AddSharedComponent(index, new MarkerCellName
                        {
                            Value = cellNames[index].Value
                        });                        
                        //CommandBuffer.AddComponent(index, new Active { active = 1 });

                        markersAdded++;

                        if(CrowdExperiment.instance.showMarkers)CommandBuffer.AddSharedComponent(index, MarkerRenderer);
                        tempCellMarkers.Add(new Position { Value = new float3(x, y, z)});
                    }
                    else
                    {
                        flag++;
                        i--;
                    }
                    if (flag > qtdMarkers * 2)
                    {
                        flag = 0;
                        break;
                    }
                }
                tempCellMarkers.Dispose();
            }

        }

        protected override void OnStartRunning()
        {
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();
            MakerArchetype = entityManager.CreateArchetype(
               ComponentType.Create<Position>(),
               ComponentType.Create<CellName>(),
               //ComponentType.Create<Active>(),
               ComponentType.Create<MarkerData>());
            MarkerRenderer = BioCrowdsBootStrap.GetLookFromPrototype("MarkerMesh");

            //if (!Settings.instance.markerSettings || Settings.instance.markerSettings.seeds.Length != cellData.Length)
            //{
            //    Settings.instance.markerSettings.seeds = new int[cellData.Length];

            //    for (int i = 0; i < cellData.Length; i++)
            //    {
            //        Settings.instance.markerSettings.seeds[i] = DateTime.UtcNow.Millisecond;
            //    }

            //}

            //Seeds = new NativeArray<int>(Settings.instance.markerSettings.seeds, Allocator.Persistent);


        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var SpawnJob = new SpawnMarkers
            {
                cellNames = cellData.CellName,
                CellPos = cellData.CellPos,
                CommandBuffer = m_SpawnerBarrier.CreateCommandBuffer().ToConcurrent(),
                SpawnData = spawnParameters.SpawnData
                //Seeds = Seeds
            };

            var SpawnJobHandle = SpawnJob.Schedule(cellData.Length, SimulationConstants.instance.BatchSize, inputDeps);

            SpawnJobHandle.Complete();

            UpdateInjectedComponentGroups();

           


            this.Enabled = false;

            return SpawnJobHandle;
        }
    }


}
