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
    /// Helper System. Determines the quantity of interested cells for each cloud.
    /// Computes the total number of desired captured markers per cloud.
    /// </summary>
    [UpdateBefore(typeof(PostMarkGroup))]
    public class CloudTagDesiredQuantitySystem : JobComponentSystem
    {

        //Data structure sizes
        int lastsize_cloudtagquantity;

        public NativeArray<int> CloudDesiredCellTagQuantity;
        public int TotalTags;

        public struct CloudGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;

        }
        [Inject] private CloudGroup m_CloudGroup;

        struct CellQuantityJob : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [WriteOnly] public NativeArray<int> cloud2Quantity;

            //[WriteOnly] public NativeQueue<DoublePosition>.Concurrent aux_draw;

            public void Execute(int index)
            {
                cloud2Quantity[index] =  GridConverter.QuantityInRadius(Position[index].Value, CloudData[index].Radius);
            }
        }


        protected override void OnDestroyManager()
        {
            if(CloudDesiredCellTagQuantity.IsCreated)
                CloudDesiredCellTagQuantity.Dispose();
        }
        protected override void OnStartRunning()
        {

            CloudDesiredCellTagQuantity = new NativeArray<int>(m_CloudGroup.Length, Allocator.Persistent);
            lastsize_cloudtagquantity = 0;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (lastsize_cloudtagquantity != m_CloudGroup.Length)
            {
                CloudDesiredCellTagQuantity.Dispose();
                CloudDesiredCellTagQuantity = new NativeArray<int>(m_CloudGroup.Length, Allocator.Persistent);
                lastsize_cloudtagquantity = m_CloudGroup.Length;
            }

            
            CellQuantityJob job = new CellQuantityJob()
            {
                CloudData = m_CloudGroup.CloudData,
                Position = m_CloudGroup.Position,
                cloud2Quantity = CloudDesiredCellTagQuantity
            };

            var deps = job.Schedule(m_CloudGroup.Length, 64, inputDeps);
            deps.Complete();

            TotalTags = 0;
            for(int i = 0; i < m_CloudGroup.Length; i++)
            {
                TotalTags += CloudDesiredCellTagQuantity[i];
            }

            return deps;
        }

    }
}