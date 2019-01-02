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

namespace BioCities {
    
    public struct HeatQuad : IComponentData { } //Marker Component

    [UpdateAfter(typeof(CellMarkSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    public class CloudHeatMap : JobComponentSystem
    {
        //Data structure size data.
        int lastsize_quadQuantities;
        int lastsize_quadIndex;
        int lastsize_texmat;

        #region Data Recording
        
        public List<Record> records;
        public int CurrentFrame;

        #endregion

        public static Texture2D tex;
        public NativeArray<Color> tex_mat;
        public int tex_mat_row;
        public int tex_mat_col;
        public NativeHashMap<int, float> cloudDensities;

        public struct QuadGroup
        {
            [WriteOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<HeatQuad> Quads;
            [ReadOnly] public SharedComponentDataArray<MeshInstanceRenderer> Renderer;
            [ReadOnly] public readonly int Length;
        }

        public struct CloudDataGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;
        }

        [Inject] CloudDataGroup m_CloudDataGroup;
        [Inject] CellMarkSystem m_CellMarkSystem;
        [Inject] QuadGroup m_QuadGroup;

        NativeArray<int> quadQuantities;
        NativeArray<int> quadIndex;
        int lastQuantity;
        Parameters inst;

        struct DeBufferQuads : IJobParallelFor
        {
            [WriteOnly] public ComponentDataArray<Position> QuadPositions;
            public NativeQueue<int> quadQueue;

            public void Execute(int index)
            {
                int ind = quadQueue.Dequeue();

                QuadPositions[ind] = new Position { Value = new float3(-10.0f, -10.0f, 0.0f) };
            }
        }

        struct ClearMat : IJobParallelFor
        {
            [WriteOnly] public NativeArray<Color> mat;

            public void Execute(int index)
            {
                mat[index] = Color.black;
            }
        }
        
        struct MoveHeatMapQuads : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public ComponentDataArray<Position> QuadPositions;
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;
            

            public NativeArray<int> quadQuantities;
            public NativeArray<int> quadIndexes;


            public void Execute(int index)
            {
                float3 currentCellPosition;

                NativeMultiHashMapIterator<int> it;
                int idx = quadIndexes[index];

                if (!CloudMarkersMap.TryGetFirstValue(CloudData[index].ID, out currentCellPosition, out it))
                    return;
                
                QuadPositions[idx++] = new Position { Value = currentCellPosition };
                
                while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
                //Do stuff 2
                    QuadPositions[idx++] = new Position { Value = currentCellPosition };

            }
        }


        struct FillDensityTex : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public NativeMultiHashMap<int, float3> CloudMarkersMap;
            [ReadOnly] public float CellArea;
            [WriteOnly] public NativeArray<int> cloudQuadQuantity;
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

                cloudQuadQuantity[index] = cellCount;
                float totalArea = cellCount * CellArea;
                CloudData cData = CloudData[index];
                float delta = cData.AgentQuantity / totalArea;
                Color densityColor = Parameters.Density2Color(delta, CloudData[index].ID);
                
                if (!CloudMarkersMap.TryGetFirstValue(CloudData[index].ID, out currentCellPosition, out it))
                    return;

                int2 grid_cell = GridConverter.PositionToGridCell(new float3(currentCellPosition.x, currentCellPosition.y, currentCellPosition.z));
                tex_mat[grid_cell.y * mat_cols + grid_cell.x] = densityColor;

                cloudDensities.TryAdd(CloudData[index].ID, delta);

                while (CloudMarkersMap.TryGetNextValue(out currentCellPosition, ref it))
                {
                    grid_cell = GridConverter.PositionToGridCell(new float3(currentCellPosition.x, currentCellPosition.y, currentCellPosition.z));
                    tex_mat[grid_cell.y * mat_rows + grid_cell.x] = densityColor;
                }

                
            }
        }



        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            var clearjob = new ClearMat() { mat = tex_mat };
            var clearDep = clearjob.Schedule(lastsize_texmat, 64);
            clearDep.Complete();

            if(cloudDensities.Capacity < m_CloudDataGroup.Length)
            {
                cloudDensities.Dispose();
                cloudDensities = new NativeHashMap<int, float>(m_CloudDataGroup.Length, Allocator.Persistent);
            }

            int aux = (int)(math.ceil(inst.CloudMaxRadius * 2 / inst.CellWidth));
            int size_quads = aux * aux * m_CloudDataGroup.Length;

            if (lastsize_quadQuantities != size_quads)
            {
                quadQuantities.Dispose();
                quadQuantities = new NativeArray<int>(size_quads, Allocator.Persistent);
            }
            
            if(lastsize_texmat != tex_mat_row * tex_mat_col)
            {
                tex_mat.Dispose();
                tex_mat = new NativeArray<Color>(tex_mat_row * tex_mat_col, Allocator.Persistent);
            }
            lastsize_texmat = tex_mat_row * tex_mat_col;

            if (lastsize_quadIndex != size_quads)
            {
                quadIndex.Dispose();
                quadIndex = new NativeArray<int>(size_quads, Allocator.Persistent);
            }
            lastsize_quadIndex = size_quads;
            lastsize_quadQuantities = size_quads;


            FillDensityTex FillDensityJob = new FillDensityTex()
            {
                CloudData = m_CloudDataGroup.CloudData,
                CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
                CellArea = inst.CellArea,
                cloudQuadQuantity = quadQuantities,
                mat_cols = tex_mat_col,
                mat_rows = tex_mat_row,
                tex_mat =  tex_mat,
                cloudDensities = cloudDensities.ToConcurrent()
            };

            //MoveHeatMapQuads MoveQuadsJob = new MoveHeatMapQuads()
            //{
            //    CloudData = m_CloudDataGroup.CloudData,
            //    CloudMarkersMap = m_CellMarkSystem.cloudID2MarkedCellsMap,
            //    QuadPositions = m_QuadGroup.Position,
            //    quadQuantities = quadQuantities,
            //    quadIndexes = quadIndex
            //};

            var calculateMatDeps = FillDensityJob.Schedule(m_CloudDataGroup.Length, 64, inputDeps);

            calculateMatDeps.Complete();

            //for (int i = 1; i < m_CloudDataGroup.Length; i++)
            //    quadIndex[i] = quadQuantities[i - 1] + quadIndex[i-1];

            //var MoveQuadsDeps = MoveQuadsJob.Schedule(m_CloudDataGroup.Length, 64, calculateMatDeps);



            //Data recording
            #region datarecording
            NativeMultiHashMap<int, float3> cellmap = m_CellMarkSystem.cloudID2MarkedCellsMap;
            float3 currentCellPosition;
            NativeMultiHashMapIterator<int> it;

            if ((inst.SaveDenstiies || inst.SavePositions))
            {
                if (inst.MaxSimulationFrames > CurrentFrame && CurrentFrame % inst.FramesForDataSave == 0)
                {
                    for (int i = 0; i < m_CloudDataGroup.Length; i++)
                    {
                        List<int> cellIDs = new List<int>();

                        if (!cellmap.TryGetFirstValue(m_CloudDataGroup.CloudData[i].ID, out currentCellPosition, out it))
                            continue;
                        int2 grid_cell = GridConverter.PositionToGridCell(new float3(currentCellPosition.x, currentCellPosition.y, currentCellPosition.z));
                        cellIDs.Add(GridConverter.GridCell2CellID(grid_cell));

                        while (cellmap.TryGetNextValue(out currentCellPosition, ref it))
                        {
                            grid_cell = GridConverter.PositionToGridCell(new float3(currentCellPosition.x, currentCellPosition.y, currentCellPosition.z));
                            cellIDs.Add(GridConverter.GridCell2CellID(grid_cell));
                        }


                        Record record = new Record(CurrentFrame,
                                                   m_CloudDataGroup.CloudData[i].ID,
                                                   m_CloudDataGroup.CloudData[i].AgentQuantity,
                                                   quadQuantities[i],
                                                   cellIDs,
                                                   m_CloudDataGroup.Position[i].Value
                            );

                        records.Add(record);
                    }
                }

                if (inst.MaxSimulationFrames == CurrentFrame - 1)
                {
                    using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(inst.LogFile + "Clouds.txt"))
                    {
                        foreach (Record record in records)
                            file.Write(record.ToString() + '\n');
                    }
                }


                CurrentFrame++;
            }
            #endregion


            tex.SetPixels(tex_mat.ToArray());
            tex.Apply(false);

            m_QuadGroup.Renderer[0].material.SetTexture("_DensityTex", tex);    
            //m_QuadGroup.Renderer[1].material.SetTexture("_DensityTex", tex);
            //
            //MoveQuadsDeps.Complete();

            return calculateMatDeps;
        }

        protected override void OnDestroyManager()
        {
            quadQuantities.Dispose();
            quadIndex.Dispose();
            tex_mat.Dispose();
            cloudDensities.Dispose();
        }

        protected override void OnStartRunning()
        {
            CurrentFrame = 0;
            records = new List<Record>();
            lastsize_quadQuantities = 0 ;
            lastsize_quadIndex = 0;
            lastsize_texmat = 0;

            quadQuantities = new NativeArray<int>(0, Allocator.TempJob);
            quadIndex = new NativeArray<int>(0, Allocator.TempJob);
            cloudDensities = new NativeHashMap<int, float>(0, Allocator.TempJob);
            inst = Parameters.Instance;
            tex_mat_col = inst.Cols;
            tex_mat_row = inst.Rows;

            tex = new Texture2D(tex_mat_row, tex_mat_col);
            tex_mat = new NativeArray<Color>(tex_mat_row * tex_mat_col, Allocator.TempJob);
            lastsize_texmat = tex_mat_row * tex_mat_col;
        }


    }

}