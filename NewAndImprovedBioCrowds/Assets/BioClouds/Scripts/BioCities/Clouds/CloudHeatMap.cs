using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Rendering;

namespace BioClouds {
    
    public struct HeatQuad : IComponentData { } //Marker Component

    /// <summary>
    /// Computes the correct cloud crowd densities. 
    /// Comptues the density texture for the heatmap representation.
    /// Updates the density texture.
    /// </summary>
    [UpdateAfter(typeof(CellMarkSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    public class CloudHeatMap : JobComponentSystem
    {
        //Data structure size data.
        int lastsize_texmat;
        
        /// <summary>
        /// 2D Density Texture. Each cell id is mapped to a pixel in the texture.
        /// This is the Crowd Density HeatMap.
        /// </summary>
        public static Texture2D tex;
        public NativeArray<Color> tex_mat;
        public int tex_mat_row;
        public int tex_mat_col;

        /// <summary>
        /// The mapping of Cloud ID to crowd density. Measured in agents / sqm
        /// </summary>
        public NativeHashMap<int, float> cloudDensities;

        public static MeshRenderer DensityRenderer;

        public struct CloudDataGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;
        }

        [Inject] CloudDataGroup m_CloudDataGroup;
        [Inject] CellMarkSystem m_CellMarkSystem;
        
        Parameters inst;


        struct ClearMat : IJobParallelFor
        {
            [WriteOnly] public NativeArray<Color> mat;

            public void Execute(int index)
            {
                mat[index] = Color.black;
            }
        }
        
        struct FillDensityTex : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;
            [ReadOnly] public float CellArea;
            [WriteOnly] public NativeHashMap<int, float>.Concurrent cloudDensities;
            [ReadOnly] public int mat_rows;
            [ReadOnly] public int mat_cols;
            [NativeDisableParallelForRestriction] public NativeArray<Color> tex_mat;

            //Index = per cloud
            public void Execute(int index)
            {
                float3 currentCellPosition;
                int cellCount = 0;
                NativeMultiHashMapIterator<int> it;
                

                if (!CloudMarkersMap.TryGetFirstValue(CloudData[index].ID, out currentCellPosition, out it))
                    return;
                cellCount++;

                while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
                    cellCount++;

                float totalArea = cellCount * CellArea;
                CloudData cData = CloudData[index];
                float delta = cData.AgentQuantity / totalArea;
                Color densityColor = Parameters.Density2Color(delta, CloudData[index].ID);
                
                if (!CloudMarkersMap.TryGetFirstValue(CloudData[index].ID, out currentCellPosition, out it))
                    return;

                int2 grid_cell = GridConverter.PositionToGridCell(new float3(currentCellPosition.x, currentCellPosition.y, currentCellPosition.z));
                tex_mat[grid_cell.y * mat_rows + grid_cell.x] = densityColor;
                
                cloudDensities.TryAdd(CloudData[index].ID, delta);

                while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
                {
                    grid_cell = GridConverter.PositionToGridCell(new float3(currentCellPosition.x, currentCellPosition.y, currentCellPosition.z));
                    tex_mat[grid_cell.y * mat_rows + grid_cell.x] = densityColor;
                }

                
            }
        }

        private void CleanUpDataStructures()
        {
            var clearjob = new ClearMat() { mat = tex_mat };
            var clearDep = clearjob.Schedule(lastsize_texmat, 64);
            clearDep.Complete();

            if (cloudDensities.Capacity < m_CloudDataGroup.Length)
            {
                cloudDensities.Dispose();
                cloudDensities = new NativeHashMap<int, float>(m_CloudDataGroup.Length, Allocator.Persistent);
            }
            else
            {
                cloudDensities.Clear();
            }


            if (lastsize_texmat != tex_mat_row * tex_mat_col)
            {
                tex_mat.Dispose();
                tex_mat = new NativeArray<Color>(tex_mat_row * tex_mat_col, Allocator.Persistent);
            }
            lastsize_texmat = tex_mat_row * tex_mat_col;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            CleanUpDataStructures();

            FillDensityTex FillDensityJob = new FillDensityTex()
            {
                CloudData = m_CloudDataGroup.CloudData,
                CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
                CellArea = inst.CellArea,
                mat_cols = tex_mat_col,
                mat_rows = tex_mat_row,
                tex_mat =  tex_mat,
                cloudDensities = cloudDensities.ToConcurrent()
            };



            var calculateMatDeps = FillDensityJob.Schedule(m_CloudDataGroup.Length, 64, inputDeps);

            calculateMatDeps.Complete();


            tex.SetPixels(tex_mat.ToArray());
            tex.Apply(false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            DensityRenderer.material.SetTexture("_DensityTex", tex);

            return calculateMatDeps;
        }

        protected override void OnDestroyManager()
        {
            if(tex_mat.IsCreated)
                tex_mat.Dispose();

            if(cloudDensities.IsCreated)
                cloudDensities.Dispose();
        }

        protected override void OnStartRunning()
        {
            lastsize_texmat = 0;
            
            cloudDensities = new NativeHashMap<int, float>(0, Allocator.Persistent);
            inst = Parameters.Instance;
            tex_mat_col = inst.Cols;
            tex_mat_row = inst.Rows;

            tex = new Texture2D(tex_mat_row, tex_mat_col);
            tex_mat = new NativeArray<Color>(tex_mat_row * tex_mat_col, Allocator.Persistent);
            lastsize_texmat = tex_mat_row * tex_mat_col;
        }


    }

}