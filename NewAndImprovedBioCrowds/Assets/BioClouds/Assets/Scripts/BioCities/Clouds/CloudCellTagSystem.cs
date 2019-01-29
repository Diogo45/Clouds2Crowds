using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
namespace BioCities
{

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
        public NativeMultiHashMap<int, int> cellTagMap;
        public NativeHashMap<int, CloudIDPosRadius> cloudIDPositions;
        public NativeArray<int> tagQuantityByCloud;
        
        //public NativeQueue<int> edgeAux;
        //public NativeList<int> cellIds;


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

            //[WriteOnly] public NativeHashMap<int, int>.Concurrent Id2Index;

            //[WriteOnly] public NativeQueue<int>.Concurrent aux;

            public void Execute(int index)
            {
                var celllist = GridConverter.RadiusInGrid(Position[index].Value, CloudData[index].Radius);

                //Debug.Log(celllist.Length);

                var cloudId = CloudData[index].ID;

                tagQuantity[index] = celllist.Length;
                cloudPos.TryAdd(CloudData[index].ID, new CloudIDPosRadius() { position = Position[index].Value, ID = CloudData[index].ID, Radius = CloudData[index].Radius, MinRadius = CloudData[index].MinRadius });
                //Id2Index.TryAdd(CloudData[index].ID, index);

                foreach (int i in celllist)
                {
                    if (cellIDmap.TryGetValue(i, out float3 cellPos))
                        if (math.distance(cellPos, Position[index].Value) >= CloudData[index].Radius)
                            continue;
                    cellTagMap.Add(i, cloudId);

                    //buffer.AddComponent<MarkedCell>(cellEntity, new MarkedCell() { });
                    //Debug
                    // aux.Enqueue(1);

                }

            }
        }



        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            //ClearMapLists clearListsJob = new ClearMapLists { tagMap = cellTagMap };
            //var clearListDep = clearListsJob.Schedule<ClearMapLists>(GridConverter.CellQuantity, 64, inputDeps);
            int cellTagMap_size = (int)m_cloudTagDesiredQuantitySystem.TotalTags *2 ;
            //Debug.Log(cellTagMap_size);
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
                //tagQuantityByCloud = new NativeArray<int>(m_tagCloudGroup.Length, Allocator.Temp);
                tagQuantityByCloud = new NativeArray<int>(m_tagCloudGroup.Length, Allocator.Persistent);
            }
            lastsize_tagQuantityByCloud = m_tagCloudGroup.Length;

            if (lastsize_cloudIDPositions != m_tagCloudGroup.Length)
            {
                cloudIDPositions.Dispose();
                //cloudIDPositions = new NativeHashMap<int, CloudIDPosRadius>(m_tagCloudGroup.Length, Allocator.Temp);
                cloudIDPositions = new NativeHashMap<int, CloudIDPosRadius>(m_tagCloudGroup.Length, Allocator.Persistent);
            }
            else
                cloudIDPositions.Clear();
            lastsize_cloudIDPositions = m_tagCloudGroup.Length;
            
            // edgeAux.Dispose();

            //Debug
            //edgeAux = new NativeQueue<int>(Allocator.TempJob);

            FillMapLists fillMapListsJob = new FillMapLists
            {
                cellTagMap = cellTagMap.ToConcurrent(),
                tagQuantity = tagQuantityByCloud,
                CloudData = m_tagCloudGroup.CloudData,
                Position = m_tagCloudGroup.Position,
                cloudPos = cloudIDPositions.ToConcurrent(),
                cellIDmap = m_cellIdMapSystem.cellId2Cellfloat3
                //Id2Index = id2Index,

                //Debug
                // aux = edgeAux
            };
            var fillMapDep = fillMapListsJob.Schedule(m_tagCloudGroup.Length, 64, inputDeps);

            fillMapDep.Complete();


            
            //Debug.Log("count: " + edgeAux.Count);

            return fillMapDep;
        }

        protected override void OnStartRunning()
        {
            lastsize_cellTagMap = 0;
            lastsize_tagQuantityByCloud = 0;
            lastsize_cloudIDPositions = 0;

            cellTagMap = new NativeMultiHashMap<int, int>(0, Allocator.Persistent);
            //tagQuantityByCloud = new NativeArray<int>(0, Allocator.Temp);
            //cloudIDPositions = new NativeHashMap<int, CloudIDPosRadius>(0, Allocator.Temp);
            tagQuantityByCloud = new NativeArray<int>(0, Allocator.Persistent);
            cloudIDPositions = new NativeHashMap<int, CloudIDPosRadius>(0, Allocator.Persistent);
            

            //Debug
            //edgeAux = new NativeQueue<int>(Allocator.TempJob);
        }

        protected override void OnDestroyManager()
        {
            tagQuantityByCloud.Dispose();
            cellTagMap.Dispose();
            cloudIDPositions.Dispose();

            //Debug
            //edgeAux.Dispose();
        }

    }

}
