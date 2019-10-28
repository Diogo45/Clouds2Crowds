using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BioCrowds
{

    [DisableAutoCreation]
    public class SpringSystem : JobComponentSystem
    {

        private float sk = 50f;
        private float skd = 3f;

        public struct MassPoint : IComponentData
        {
            public float mass;
        }

        public struct Spring
        {
            //ID of agent 1
            public int m1;
            //ID of agent 2
            public int m2;

            public float k;
            public float kd;
        }

        public NativeList<Spring> springs;

        protected override void OnCreateManager()
        {
            springs = new NativeList<Spring>(Settings.experiment.SpringConnections.Length, Allocator.Persistent);
        }


        protected override void OnStartRunning()
        {
            springs.Clear();
            foreach(int2 s in Settings.experiment.SpringConnections)
            {
                springs.Add(new Spring { k = sk, kd = skd, m1 = s.x, m2 = s.y });
            }


        }





    }
}