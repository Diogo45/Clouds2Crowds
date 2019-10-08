using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEditor;

namespace BioClouds
{
    [UpdateAfter(typeof(CloudSplitSystem))]
    //[DisableAutoCreation]
    public class CloudMergeSystem : ComponentSystem
    {
        public struct CloudGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<CloudGoal> CloudGoal;
            public ComponentDataArray<CloudSplitData> CloudSplitData;
            [ReadOnly] public ComponentDataArray<CloudMoveStep> CloudStep;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public readonly int Length;
        }
        [Inject] public CloudGroup m_CloudGroup;
        [Inject] public CloudRightPreferenceSystem m_RightPreference;
        [Inject] public CellMarkSystem m_CellMarks;
        [Inject] public CellIDMapSystem m_CellIdMapSystem;

        public BioClouds bioClouds;
        //Injected from BioClouds
        public HashSet<int> CreatedCellsMap;


        public float merge_angle = 0.20f;


        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            bioClouds = GameObject.FindObjectOfType<BioClouds>();


        }
        protected override void OnUpdate()
        {

            HashSet<int> merged_ids = new HashSet<int>();


            for (int i = 0; i < m_CloudGroup.Length; i++)
            {

                var split_data = m_CloudGroup.CloudSplitData[i];
                split_data.CloudSplitTimer -= 1;
                m_CloudGroup.CloudSplitData[i] = split_data;

            }




            for (int i = 0; i < m_CloudGroup.Length; i ++)
            {
                if (merged_ids.Contains(i))
                    continue;


                for (int j = i+1; j < m_CloudGroup.Length; j++)
                {
                    if ( merged_ids.Contains(i))
                        break;

                    if (merged_ids.Contains(j))
                        continue;
                    
                    if (DecideMerge(i, j))
                    {
                       // Debug.Log("Merge Clouds" + i + " " + j);
                        MergeClouds(i, j);
                        merged_ids.Add(j);
                        merged_ids.Add(i);
                    }
                }

            }

        }

        private bool DecideMerge(int smallerIdIndex, int largerIdIndex)
        {
            float3 smallerIdPosition = m_CloudGroup.Position[smallerIdIndex].Value;
            float3 largerIdPosition = m_CloudGroup.Position[largerIdIndex].Value;

            var smallerCloudId = m_CloudGroup.CloudData[smallerIdIndex].ID;
            var largerCloudId = m_CloudGroup.CloudData[largerIdIndex].ID;

            var smallerIDSplit = m_CloudGroup.CloudSplitData[smallerIdIndex];
            var largerIDSplit = m_CloudGroup.CloudSplitData[largerIdIndex];

            var line_cells = GridConverter.LineInGrid(smallerIdPosition, largerIdPosition);

            var cell_map = m_CellMarks.Cell2OwningCloud;

            var smallerCloudType = m_CloudGroup.CloudData[smallerIdIndex].Type;
            var largerCloudType = m_CloudGroup.CloudData[largerIdIndex].Type;

            if (smallerIDSplit.CloudSplitTimer > 0 || largerIDSplit.CloudSplitTimer > 0) return false;

            if (smallerCloudType != largerCloudType) return false;

            var smallerIdCloudDirection = m_CloudGroup.CloudStep[smallerIdIndex].Delta;
            var largerIdCloudDirection = m_CloudGroup.CloudStep[largerIdIndex].Delta;

            if (math.length(smallerIdCloudDirection) < 0.1f && math.length(largerIdCloudDirection) < 0.1f)
                return false;

            //if (math.abs((math.dot(smallerIdCloudDirection, largerIdCloudDirection) / (math.length(smallerIdCloudDirection) * math.length(largerIdCloudDirection)))) > merge_angle)
            //    return false;

            if (math.abs(math.length(math.cross(smallerIdCloudDirection, largerIdCloudDirection) / (math.length(smallerIdCloudDirection) * math.length(largerIdCloudDirection)))) > merge_angle)
                return false;

            
            foreach (int cell_id in line_cells)
            {
                if (m_CellIdMapSystem.cellId2Cellfloat3.TryGetValue(cell_id, out float3 cellPos))
                {
                    //Debug.Log("Line " + cell_id + " " + cellPos);
                    Debug.DrawLine(smallerIdPosition, cellPos);
                }
                //if avaiable, test if id is different.


                if (!(cell_map.TryGetValue(cell_id, out int result) && (result == smallerCloudId || result == largerCloudId)))
                {
                    return false;
                }

            }
            return true;

        }

        private void MergeClouds(int smallerIdCloud, int largerIdCloud)
        {

            var cell_map = bioClouds.created_cell_ids;

            CloudData smallerIdCloudData = m_CloudGroup.CloudData[smallerIdCloud];
            CloudData largerIdCloudData = m_CloudGroup.CloudData[largerIdCloud];


            CloudSplitData smallerIdfatherData = m_CloudGroup.CloudSplitData[smallerIdCloud];
            CloudSplitData largerIdfatherData = m_CloudGroup.CloudSplitData[largerIdCloud];

            float3 smallerIdPosition = m_CloudGroup.Position[smallerIdCloud].Value;
            float3 largerIdPosition = m_CloudGroup.Position[largerIdCloud].Value;

            int total_agents = smallerIdCloudData.AgentQuantity + largerIdCloudData.AgentQuantity;

            float3 resultingPosiion = (smallerIdPosition * smallerIdCloudData.AgentQuantity + largerIdPosition * largerIdCloudData.AgentQuantity) / total_agents;

            


            //float density = (smallerIdCloudData.PreferredDensity * smallerIdCloudData.AgentQuantity) + (largerIdCloudData.PreferredDensity * largerIdCloudData.AgentQuantity);
            //density /= total_agents;

            float density = smallerIdCloudData.PreferredDensity;

            float total_radius = BioClouds.CloudPreferredRadius(total_agents, density);

            int split_count = Mathf.Max(smallerIdfatherData.splitCount, largerIdfatherData.splitCount) - 1;


            CloudLateSpawn lateSpawn = new CloudLateSpawn();
   
            lateSpawn.goal = m_CloudGroup.CloudGoal[smallerIdCloud].EndGoal;
            lateSpawn.cloudType = smallerIdCloudData.Type;
            lateSpawn.preferredDensity = density;
            lateSpawn.radiusChangeSpeed = smallerIdCloudData.RadiusChangeSpeed;
            lateSpawn.splitCount = split_count;
            lateSpawn.fatherID = smallerIdfatherData.fatherID;
            lateSpawn.position = resultingPosiion;
            lateSpawn.agentQuantity = total_agents;
            
            bioClouds.entitiesToDestroy.Add(m_CloudGroup.Entities[smallerIdCloud]);
            bioClouds.entitiesToDestroy.Add(m_CloudGroup.Entities[largerIdCloud]);

            bioClouds.cloudLateSpawns.Add(lateSpawn);

        }
    }
    
}