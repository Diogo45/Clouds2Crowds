using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;



namespace BioClouds
{

  /// <summary>
  /// Each tagged cell marks the cloud closest to them.
  /// Produces a map of Cloud ID to list of uniquely marked Cells.
  /// </summary>
    [UpdateAfter(typeof(CloudCellTagSystem))]
    public class CellMarkSystem : JobComponentSystem
    {
        
        struct DoublePosition
        {
            public float3 pos1;
            public float3 pos2;
        }


        //Holds the positions of eac tagged cell. Paired with the indexed cloud vector.
        /// <summary>
        /// The mapping of Cloud ID to uniquely marked cells.
        /// </summary>
        public NativeMultiHashMap<int, float3> cloudID2MarkedCellsMap;
        //Data structure sizes
        int lastsize_cloudID2MarkedCellsMap;

        /// <summary>
        /// The mapping of Cell ID to owning cloud.
        /// </summary>
        public NativeHashMap<int, int> Cell2OwningCloud;
        int lastsize_cell2owningcloud;


        //Each cell chooses from among the tags for the closest agent

        public struct MarkedCellsGroup
        {
            [ReadOnly] public ComponentDataArray<CellData> Cell;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public EntityArray CellEntities;
            [ReadOnly] public readonly int Length;
            
        }
        [Inject] private MarkedCellsGroup m_MarkedCellsgGroup;

        public struct CloudGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public readonly int Length;
        }
        [Inject] private CloudGroup m_CloudGroup;

        [Inject] CloudCellTagSystem m_cloudCellTagsSystem;


        /// <summary>
        /// This job fills the CloudID -> Marked cell and the Cell ID -> owning cloud relations.
        /// </summary>
        struct MarkCellsNotifyAgentsJob : IJobParallelFor
        {
            [WriteOnly] public NativeMultiHashMap<int, float3>.Concurrent cloudID2MarkedCellsMap;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public NativeHashMap<int, CloudIDPosRadius> cloudIDPositions;

            [ReadOnly] public NativeMultiHashMap<int, int> cellTagMap;
            [ReadOnly] public ComponentDataArray<CellData> CellData;

            //Cell 2 Cloud Map
            [WriteOnly] public NativeHashMap<int, int>.Concurrent Cell2OwnCloud;



            public float EuclidianMark(float3 target, float3 pos)
            {
                return math.distance(target, pos);
            }


            public float PowerDistanceMark(float3 target, float3 pos, float radius)
            {
                var s = math.distance(target, pos);

                return math.pow(s, 2) - math.pow(radius, 2);
            }


            public float CaptureDistanceFunction(float3 target, float3 current, float radius, float minradius)
            {
                return PowerDistanceMark(target, current, radius);
            }


            public void Execute(int cellGroupIndex)
            {
                int cellId = CellData[cellGroupIndex].ID;

                NativeMultiHashMapIterator<int> it;
                int currentCloudId = -1;

                int closestId = -1;
                float closestDistance = float.PositiveInfinity;

                bool keepgoing = cellTagMap.TryGetFirstValue(cellId, out currentCloudId, out it);

                if (!keepgoing)
                    return;


                CloudIDPosRadius currentCloud;
                cloudIDPositions.TryGetValue(currentCloudId, out currentCloud);

                float aux_dist = math.distance(currentCloud.position, Position[cellGroupIndex].Value);

                if (aux_dist <= currentCloud.Radius)
                {
                    closestDistance = CaptureDistanceFunction(Position[cellGroupIndex].Value, currentCloud.position, currentCloud.Radius, currentCloud.MinRadius);
                    closestId = currentCloudId;

                }

                while (cellTagMap.TryGetNextValue(out currentCloudId, ref it))
                {

                    cloudIDPositions.TryGetValue(currentCloudId, out currentCloud);
                    aux_dist = math.distance(currentCloud.position, Position[cellGroupIndex].Value);
                    var cap_dist = CaptureDistanceFunction(Position[cellGroupIndex].Value, currentCloud.position, currentCloud.Radius, currentCloud.MinRadius);

                    if (aux_dist <= currentCloud.Radius && cap_dist <= closestDistance)
                    {
                        
                        if (cap_dist != closestDistance)
                        {
                            closestDistance = cap_dist;
                            closestId = currentCloudId;
                        }
                        else
                        {
                            if (closestId > currentCloudId)
                            {
                                closestDistance = cap_dist;
                                closestId = currentCloudId;
                            }

                        }

                    }

                }

                if (closestId == -1)
                    return;

                Cell2OwnCloud.TryAdd(cellId, closestId);
                cloudID2MarkedCellsMap.Add(closestId, Position[cellGroupIndex].Value);
            }
        }


        protected override void OnDestroyManager()
        {
            if(cloudID2MarkedCellsMap.IsCreated)
                cloudID2MarkedCellsMap.Dispose();

            if(Cell2OwningCloud.IsCreated)
                Cell2OwningCloud.Dispose();


        }
        protected override void OnStartRunning()
        {

            cloudID2MarkedCellsMap = new NativeMultiHashMap<int, float3>(0, Allocator.Persistent);
            lastsize_cloudID2MarkedCellsMap = 0;

            Cell2OwningCloud = new NativeHashMap<int, int>(0, Allocator.Persistent);
            lastsize_cell2owningcloud = 0;

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            int total = 0;
            for (int i = 0; i < m_CloudGroup.Length; i++)
            {
                total += m_cloudCellTagsSystem.tagQuantityByCloud[i];
            }

            int mapsize = (int)(total);
            int mapsizeupd = (int)(mapsize * 1.1f);

            if (cloudID2MarkedCellsMap.Length < mapsize )
            {
                cloudID2MarkedCellsMap.Dispose();
                cloudID2MarkedCellsMap = new NativeMultiHashMap<int, float3>(mapsizeupd, Allocator.Persistent);
            }
            else
                cloudID2MarkedCellsMap.Clear();

            lastsize_cloudID2MarkedCellsMap = mapsizeupd;

            if(lastsize_cell2owningcloud != m_MarkedCellsgGroup.Length)
            {
                Cell2OwningCloud.Dispose();
                Cell2OwningCloud = new NativeHashMap<int, int>(m_MarkedCellsgGroup.Length, Allocator.Persistent);
                lastsize_cell2owningcloud = m_MarkedCellsgGroup.Length;
            }
            else
            {
                Cell2OwningCloud.Clear();
            }

            MarkCellsNotifyAgentsJob markCellsJob = new MarkCellsNotifyAgentsJob()
            {
                cloudID2MarkedCellsMap = cloudID2MarkedCellsMap.ToConcurrent(),
                Position = m_MarkedCellsgGroup.Position,
                cloudIDPositions = m_cloudCellTagsSystem.cloudIDPositions,
                cellTagMap = m_cloudCellTagsSystem.cellTagMap,
                CellData = m_MarkedCellsgGroup.Cell,
                Cell2OwnCloud = Cell2OwningCloud.ToConcurrent()

            };

            var markCellsDep = markCellsJob.Schedule(m_MarkedCellsgGroup.Length, 64, inputDeps);

            markCellsDep.Complete();

            return markCellsDep;
        }

    }
}