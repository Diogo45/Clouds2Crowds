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
    [UpdateAfter(typeof(CloudRightPreferenceSystem))]
    //[DisableAutoCreation]
    public class CloudSplitSystem : ComponentSystem
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

        public BioClouds bioClouds;
        //Injected from BioClouds
        public HashSet<int> CreatedCellsMap;

        public int divisions = 6;
        public float spawnDistanceFromRadius = 0.3f;
        public float angleThreshold = 120.0f;
        public float magnitudeRadiusThreshold = 3.0f;
        public float squishiness_threshold = 0.7f;
        public float split_threshold = 0.9f;
        public bool rotateHalfSlice = true;
        public float framesForward = 15f;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            bioClouds = GameObject.FindObjectOfType<BioClouds>();

        }
        protected override void OnUpdate()
        {

            for (int i = 0; i < m_CloudGroup.Length; i ++)
            {
                //Debug.DrawLine(m_CloudGroup.Position[i].Value,
                //    m_CloudGroup.Position[i].Value + m_RightPreference.dessums[i],
                //    Color.green);
                //Debug.DrawLine(m_CloudGroup.Position[i].Value,
                //    m_CloudGroup.Position[i].Value + m_RightPreference.sums[i],
                //    Color.yellow);

                var split_data = m_CloudGroup.CloudSplitData[i];
                split_data.CloudSplitTimer -= 1;
                m_CloudGroup.CloudSplitData[i] = split_data;



                if (m_CloudGroup.CloudSplitData[i].CloudSplitTimer <= 0 &&
                    m_CloudGroup.CloudSplitData[i].splitCount < m_CloudGroup.CloudSplitData[i].CloudSplitLimit &&
                    m_CloudGroup.CloudData[i].AgentQuantity > m_CloudGroup.CloudSplitData[i].CloudSizeLimit)
                    {

                    if (DecideSplit(i))
                        SplitCloud(i);

                }  
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                SplitCloud(0);
            }

            else if (Input.GetKeyDown(KeyCode.Backspace))
            {
                bioClouds.DestroyCloud(m_CloudGroup.Entities[0]);
            }
        }

        private bool DecideSplit(int index)
        {


            float3 future_position = m_CloudGroup.Position[index].Value + m_CloudGroup.CloudStep[index].Delta * framesForward;

            var future_cells = GridConverter.RadiusInGrid(future_position, m_CloudGroup.CloudData[index].Radius);
            var current_cells = GridConverter.RadiusInGrid(m_CloudGroup.Position[index].Value, m_CloudGroup.CloudData[index].Radius);

            int avaiable_future_cells = 0;
            int avaiable_current_cells = 0;
            var cell_map = m_CellMarks.Cell2OwningCloud;


            foreach (int cell_id in current_cells)
            {
                //if avaiable, test if id is different.
                if ((!cell_map.TryGetValue(cell_id, out int result) || result == m_CloudGroup.CloudData[index].ID) && bioClouds.created_cell_ids.Contains(cell_id))
                {
                    avaiable_current_cells++;

                }
            }

            foreach (int cell_id in future_cells)
            {
                //if avaiable, test if id is different.
                if((!cell_map.TryGetValue(cell_id, out int result) || result == m_CloudGroup.CloudData[index].ID) && bioClouds.created_cell_ids.Contains(cell_id))
                {
                    avaiable_future_cells++;
                }
            }
            float split_value = ((float)avaiable_future_cells / (float)avaiable_current_cells);
            //Debug.Log(avaiable_future_cells + " " + avaiable_current_cells + " " + split_value + " " + (split_value < split_threshold));
            return split_value < split_threshold;
        }

        private void SplitCloud(int index)
        {

            var cell_map = bioClouds.created_cell_ids;

            Debug.Log("Split cloud");
            CloudData data = m_CloudGroup.CloudData[index];
            CloudSplitData fatherData = m_CloudGroup.CloudSplitData[index];

            float3 basePosition = m_CloudGroup.Position[index].Value;
            float3 offset;
            float slice = (360.0f / (float)(divisions));

            int total_agents = data.AgentQuantity;
            int agents_slice = Mathf.CeilToInt(data.AgentQuantity / (divisions+1));


            for (int i = 0; i <= divisions; i++)
            {
                if (total_agents <= 0)
                    break;

                float lolcalSpawnDistanceFromRadius = m_CloudGroup.CloudData[index].Radius * 0.5f;

                offset.x = math.cos(math.radians(((slice * (i)) + (slice / 2f))));
                offset.y = math.sin(math.radians(((slice * (i)) + (slice / 2f))));
                offset.z = 0f;
                offset *= ( lolcalSpawnDistanceFromRadius);
                CloudLateSpawn lateSpawn = new CloudLateSpawn();

                lateSpawn.agentQuantity = math.min(agents_slice, total_agents);
                if (i == divisions)
                    lateSpawn.position = basePosition;
                else
                    lateSpawn.position = basePosition + offset;


                if (!cell_map.Contains(GridConverter.Position2CellID(lateSpawn.position)))
                    continue;


                total_agents -= lateSpawn.agentQuantity;

                if (i == divisions)
                    lateSpawn.agentQuantity += total_agents;

                
                lateSpawn.end_goal_id = m_CloudGroup.CloudGoal[index].EndObjectiveID;
                lateSpawn.current_goal_id = m_CloudGroup.CloudGoal[index].CurrentObjectiveID;
                lateSpawn.cloudType = data.Type;
                lateSpawn.preferredDensity = data.PreferredDensity;
                lateSpawn.radiusChangeSpeed = data.RadiusChangeSpeed;
                lateSpawn.splitCount = m_CloudGroup.CloudSplitData[index].splitCount + 1;
                lateSpawn.fatherID = fatherData.fatherID;

                //if (total_agents != 0 && i == divisions)
                //    lateSpawn.agentQuantity += total_agents;

                bioClouds.cloudLateSpawns.Add(lateSpawn);
            }
            bioClouds.entitiesToDestroy.Add(m_CloudGroup.Entities[index]);
        }
    }
    
}