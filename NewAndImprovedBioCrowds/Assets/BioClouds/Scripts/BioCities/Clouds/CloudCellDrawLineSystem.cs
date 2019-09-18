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
    /// Debug System. Draws Lines from Cloud center to captured Cells.
    /// </summary>
    [UpdateAfter(typeof(CellMarkSystem))]
    [UpdateInGroup(typeof(PostMarkGroup))]
    public class CloudCellDrawLineSystem : ComponentSystem
    {
        Color[] colors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow};
        public struct CloudDataGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public readonly int Length;
        }
        [Inject] CloudDataGroup m_CloudDataGroup;

        [Inject] CellMarkSystem m_CellMarkSystem;


        protected override void OnUpdate()
        {

            NativeMultiHashMap<int, float3> cloud2CellPos = m_CellMarkSystem.cloudID2MarkedCellsMap;
            NativeMultiHashMapIterator<int> it;
            float3 currentCellPosition;



            for (int i = 0; i < m_CloudDataGroup.Length; i++)
            {
                bool keepgoing = cloud2CellPos.TryGetFirstValue(m_CloudDataGroup.CloudData[i].ID, out currentCellPosition, out it);

                if (keepgoing)
                {
                    Debug.DrawLine(m_CloudDataGroup.Position[i].Value, currentCellPosition, colors[m_CloudDataGroup.CloudData[i].Type]);

                    while (cloud2CellPos.TryGetNextValue(out currentCellPosition, ref it))
                    {
                        Debug.DrawLine(m_CloudDataGroup.Position[i].Value, currentCellPosition, colors[m_CloudDataGroup.CloudData[i].Type]);
                    }

                }
            }
        }
    }

}