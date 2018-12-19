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

    public struct SpawnData : IComponentData
    {
        
        public int qtdPerCell;
    }

    [UpdateBefore(typeof(CellTagSystem)), UpdateAfter(typeof(MarkerSpawnSystem))]
    public class SpawnBarrier : BarrierSystem { }


    [UpdateBefore(typeof(CellTagSystem))]
    public class MarkerSpawnSystem : JobComponentSystem
    {
        [Inject] private SpawnBarrier m_SpawnerBarrier;
        public static MeshInstanceRenderer MarkerRenderer;

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


        struct SpawnMarkers : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<Position> CellPos;
            [ReadOnly] public ComponentDataArray<CellName> cellNames;
            public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ComponentDataArray<SpawnData> SpawnData;


            public void Execute(int index)
            {
                int qtdMarkers = SpawnData[0].qtdPerCell;
                int flag = 0;
                int markersAdded = 0;
                NativeList<Position> tempCellMarkers = new NativeList<Position>(qtdMarkers, Allocator.Persistent);
                System.Random r = new System.Random(DateTime.UtcNow.Millisecond);

                for (int i = 0; i < qtdMarkers; i++)
                {

                    //FUTURE: Add a proper 'y' coordinate
                    float x = ((float)r.NextDouble()*2f - 1f) + cellNames[index].Value.x;
                    float y = 0f;
                    float z = ((float)r.NextDouble()*2f - 1f) + cellNames[index].Value.z;

                    bool canInstantiate = true;

                    for (int j = 0; j < tempCellMarkers.Length; j++)
                    {
                        float distanceAA = math.distance(new float3(x, y, z), tempCellMarkers[j].Value);
                        if (distanceAA < Settings.instance.markerRadius)
                        {
                            canInstantiate = false;
                            break;
                        }
                    }
                   
                    if (canInstantiate)
                    {
                        //Debug.Log("Instaciou uma");
                        CommandBuffer.CreateEntity(index, MakerArchetype);

                        CommandBuffer.SetComponent(index, new Position
                        {
                            Value = new float3(x, y, z)
                        });
                        CommandBuffer.SetComponent(index, new MarkerData
                        {
                            id = markersAdded
                        });
                        CommandBuffer.SetComponent(index, new CellName
                        {
                            Value = cellNames[index].Value
                        });
                        CommandBuffer.SetComponent(index, new Active { active = 1 });


                        markersAdded++;

                        if(Settings.instance.showMarkers)CommandBuffer.AddSharedComponent(index, MarkerRenderer);
                        tempCellMarkers.Add(new Position { Value = new float3(x, y, z)});
                        //temptemp.Add(newMarker);
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
               ComponentType.Create<Active>(),
               ComponentType.Create<MarkerData>());
            MarkerRenderer = BioCrowdsBootStrap.GetLookFromPrototype("MarkerMesh");

        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var SpawnJob =  new SpawnMarkers
            {
                cellNames = cellData.CellName,
                CellPos = cellData.CellPos,
                CommandBuffer = m_SpawnerBarrier.CreateCommandBuffer().ToConcurrent(),
                SpawnData = spawnParameters.SpawnData
            };

            var SpawnJobHandle = SpawnJob.Schedule(cellData.Length, Settings.BatchSize, inputDeps);

            SpawnJobHandle.Complete();

            this.Enabled = false;

            return SpawnJobHandle;
        }
    }


}
