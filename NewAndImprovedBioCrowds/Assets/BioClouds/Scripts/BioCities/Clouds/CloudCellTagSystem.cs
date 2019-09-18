using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
namespace BioClouds
{

    /// <summary>
    /// Cell tagging system. 
    /// Each Cloud notifies their interest into capturing a certain region of space by tagging cells in that region.
    /// This system produces a mapping of Cell ID to list of IDs of interested Clouds.
    /// This system produces a mapping of Cloud ID to a tuple (Cloud position, Cloud Radius).
    /// </summary>
    [UpdateAfter(typeof(CellIDMapSystem))]
    [UpdateAfter(typeof(CloudTagDesiredQuantitySystem))]
    public class CloudCellTagSystem : JobComponentSystem
    {
        //parameters
        Parameters inst = Parameters.Instance;

        //Data structure size data.
        int lastsize_cellTagMap;
        int lastsize_tagQuantityByCloud;
        int lastsize_cloudIDPositions;

        //Maps cellID to interested agents
        /// <summary>
        /// A mapping of interested cloud ids of interested clouds into a cell.
        /// Maps Cell ID to a list of interested Clouds.
        /// </summary>
        public NativeMultiHashMap<int, int> cellTagMap;
        /// <summary>
        /// A map of Cloud ID to a tuple (Cloud Position, Cloud Radius)
        /// </summary>
        public NativeHashMap<int, CloudIDPosRadius> cloudIDPositions;

        /// <summary>
        /// NO TOUCHY. Quantity of tagged cells. 
        /// </summary>
        public NativeArray<int> tagQuantityByCloud;

        public struct TagCloudGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public readonly int Length;
        }
        [Inject] TagCloudGroup m_tagCloudGroup;

        [Inject] CellIDMapSystem m_cellIdMapSystem;

        [Inject] CloudTagDesiredQuantitySystem m_cloudTagDesiredQuantitySystem;

        struct FillMapLists : IJobParallelFor
        {
            [WriteOnly] public NativeMultiHashMap<int, int>.Concurrent cellTagMap;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [WriteOnly] public NativeArray<int> tagQuantity;
            [WriteOnly] public NativeHashMap<int, CloudIDPosRadius>.Concurrent cloudPos;
            [ReadOnly] public NativeHashMap<int, float3> cellIDmap;

            public void Execute(int index)
            {
                var celllist = GridConverter.RadiusInGrid(Position[index].Value, CloudData[index].Radius);

                var cloudId = CloudData[index].ID;

                tagQuantity[index] = celllist.Length;
                cloudPos.TryAdd(CloudData[index].ID, new CloudIDPosRadius() { position = Position[index].Value, ID = CloudData[index].ID, Radius = CloudData[index].Radius, MinRadius = CloudData[index].MinRadius });

                foreach (int i in celllist)
                {
                    if (cellIDmap.TryGetValue(i, out float3 cellPos))
                        if (math.distance(cellPos, Position[index].Value) >= CloudData[index].Radius)
                            continue;
                    cellTagMap.Add(i, cloudId);
                }

            }
        }



        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
           
            int cellTagMap_size = (int)m_cloudTagDesiredQuantitySystem.TotalTags *2 ;
            if (lastsize_cellTagMap != cellTagMap_size)
            {
                cellTagMap.Dispose();
                cellTagMap = new NativeMultiHashMap<int, int>(cellTagMap_size, Allocator.Persistent);
            }
            else
                cellTagMap.Clear();
            lastsize_cellTagMap = cellTagMap_size;


            if (lastsize_tagQuantityByCloud != m_tagCloudGroup.Length)
            {
                tagQuantityByCloud.Dispose();

                tagQuantityByCloud = new NativeArray<int>(m_tagCloudGroup.Length, Allocator.Persistent);
            }
            lastsize_tagQuantityByCloud = m_tagCloudGroup.Length;

            if (lastsize_cloudIDPositions != m_tagCloudGroup.Length)
            {
                cloudIDPositions.Dispose();

                cloudIDPositions = new NativeHashMap<int, CloudIDPosRadius>(m_tagCloudGroup.Length, Allocator.Persistent);
            }
            else
                cloudIDPositions.Clear();
            lastsize_cloudIDPositions = m_tagCloudGroup.Length;


            FillMapLists fillMapListsJob = new FillMapLists
            {
                cellTagMap = cellTagMap.ToConcurrent(),
                tagQuantity = tagQuantityByCloud,
                CloudData = m_tagCloudGroup.CloudData,
                Position = m_tagCloudGroup.Position,
                cloudPos = cloudIDPositions.ToConcurrent(),
                cellIDmap = m_cellIdMapSystem.cellId2Cellfloat3

            };
            var fillMapDep = fillMapListsJob.Schedule(m_tagCloudGroup.Length, 64, inputDeps);

            fillMapDep.Complete();
            

            return fillMapDep;
        }

        protected override void OnStartRunning()
        {
            lastsize_cellTagMap = 0;
            lastsize_tagQuantityByCloud = 0;
            lastsize_cloudIDPositions = 0;

            cellTagMap = new NativeMultiHashMap<int, int>(0, Allocator.Persistent);

            tagQuantityByCloud = new NativeArray<int>(0, Allocator.Persistent);
            cloudIDPositions = new NativeHashMap<int, CloudIDPosRadius>(0, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            if (tagQuantityByCloud.IsCreated)
                tagQuantityByCloud.Dispose();
            if(cellTagMap.IsCreated)
                cellTagMap.Dispose();
            if(cloudIDPositions.IsCreated)
                cloudIDPositions.Dispose();
        }

    }

}
