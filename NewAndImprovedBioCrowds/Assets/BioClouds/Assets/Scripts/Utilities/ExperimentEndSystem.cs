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

    [UpdateAfter(typeof(CloudMovementVectorSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    [DisableAutoCreation]
    public class ExperimentEndSystem : ComponentSystem
    {
        //Moves based on marked cell list
        public struct CloudGroup
        {
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<CloudData> Cloud;
            [ReadOnly] public readonly int Length;
        }
        [Inject] CloudGroup m_CellGroup;

        [Inject] CellMarkSystem m_markSystem;
        int frames = 0;
        Parameters instance;
        float startingTime;
        public int[] Rulers;

        protected override void OnUpdate()
        {
            int finished = 0;
            frames ++;

            for (int i = 0; i < m_CellGroup.Length; i++) 
            {
                switch (m_CellGroup.Cloud[i].Type)
                {
                    case 0:
                        if (m_CellGroup.Position[i].Value.x - m_CellGroup.Cloud[i].Radius > Rulers[0])
                            finished++;
                        break;
                    case 1:
                        if (m_CellGroup.Position[i].Value.x + m_CellGroup.Cloud[i].Radius < Rulers[1])
                            finished++;
                        break;

                    case 2:
                        if (m_CellGroup.Position[i].Value.y - m_CellGroup.Cloud[i].Radius > Rulers[3])
                            finished++;
                        break;
                    case 3:
                        if (m_CellGroup.Position[i].Value.y + m_CellGroup.Cloud[i].Radius < Rulers[2])
                            finished++;
                        break;

                }

            }

            if (finished == m_CellGroup.Length)
                Debug.Log("simulation ended at frame: " + frames  + "_" + (Time.time - startingTime));


        }
        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            instance = Parameters.Instance;
            startingTime = Time.time;
        }


    }
}