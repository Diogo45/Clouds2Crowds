using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

namespace BioClouds
{
    [UpdateAfter(typeof(CloudMovementVectorSystem))]
    public class MaterialFixer : JobComponentSystem
    {
        public MeshRenderer renderer;

        public ComputeBuffer radius_buffer;
        public ComputeBuffer pos_buffer;

        public struct CloudDataGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;
        }

        [Inject] CloudDataGroup cloudGroup;

        protected override void OnCreateManager()
        {
            radius_buffer = new ComputeBuffer(1, sizeof(float));
            pos_buffer = new ComputeBuffer(1, sizeof(float)*3);

            base.OnCreateManager();
        }
        protected override void OnStopRunning()
        {

            radius_buffer.Dispose();
            pos_buffer.Dispose();

        
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if(radius_buffer.count != cloudGroup.Length && cloudGroup.Length != 0)
            {
                radius_buffer.Dispose();
                pos_buffer.Dispose();

                radius_buffer = new ComputeBuffer(cloudGroup.Length, sizeof(float));
                pos_buffer = new ComputeBuffer(cloudGroup.Length, sizeof(float) * 3);
            }


            float[] radius = new float[cloudGroup.Length];
            float3[] pos = new float3[cloudGroup.Length];

            for (int i = 0; i < cloudGroup.Length; i++)
            {
                radius[i] = cloudGroup.CloudData[i].Radius;
                pos[i] = cloudGroup.Position[i].Value;
            }

            radius_buffer.SetData(radius);
            pos_buffer.SetData(pos);

            renderer.material.SetBuffer("positions_buffer", pos_buffer);
            renderer.material.SetBuffer("radius_buffer", radius_buffer);
            //RenderMaterial.SetInt("_Clouds", cloudGroup.Length);


            return base.OnUpdate(inputDeps);
        }

    }
}