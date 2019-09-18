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
    /// Creates a map of Cell ID to Cell Position.
    /// Updates map whenever cell quantity changes.
    /// Inject to obtain updated map.
    /// </summary>
    [UpdateBefore(typeof(PostMarkGroup))]
    public class CellIDMapSystem : JobComponentSystem
    {

        //Data structure sizes
        int lastsize_cellId2Cellfloat3Map;

        /// <summary>
        /// Map of Cell ID to Cell Position.
        /// </summary>
        public NativeHashMap<int, float3> cellId2Cellfloat3;

        public struct CellsGroup
        {
            [ReadOnly] public ComponentDataArray<CellData> Cell;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;

        }
        [Inject] private CellsGroup m_CellsgGroup;

        struct CellID2float3Map : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<CellData> CellData;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [WriteOnly] public NativeHashMap<int, float3>.Concurrent id2float3;

            public void Execute(int cellGroupIndex)
            {
                id2float3.TryAdd(CellData[cellGroupIndex].ID, Position[cellGroupIndex].Value);
            }
        }


        protected override void OnDestroyManager()
        {
            if(cellId2Cellfloat3.IsCreated)
                cellId2Cellfloat3.Dispose();
        }
        protected override void OnCreateManager()
        {

            cellId2Cellfloat3 = new NativeHashMap<int, float3>(m_CellsgGroup.Length, Allocator.Persistent);
            lastsize_cellId2Cellfloat3Map = m_CellsgGroup.Length;

            for (int i = 0; i < m_CellsgGroup.Length; i++)
                cellId2Cellfloat3.TryAdd(m_CellsgGroup.Cell[i].ID, m_CellsgGroup.Position[i].Value);

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (lastsize_cellId2Cellfloat3Map == m_CellsgGroup.Length)
                return inputDeps;

            cellId2Cellfloat3.Dispose();
            cellId2Cellfloat3 = new NativeHashMap<int, float3>(m_CellsgGroup.Length, Allocator.Persistent);
            lastsize_cellId2Cellfloat3Map = m_CellsgGroup.Length;
            CellID2float3Map job = new CellID2float3Map()
            {
                CellData = m_CellsgGroup.Cell,
                Position = m_CellsgGroup.Position,
                id2float3 = cellId2Cellfloat3.ToConcurrent()
            };
            var deps = job.Schedule(m_CellsgGroup.Length, 64, inputDeps);
            deps.Complete();
            return deps;
        }

    }
}