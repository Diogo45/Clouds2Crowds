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
            [ReadOnly] public ComponentDataArray<CloudSplitData> CloudSplitData;
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
        public float radiusMultiplier = 3.0f;
        public float angleThreshold = 120.0f;
        public float magnitudeRadiusThreshold = 3.0f;
        public float squishiness_threshold = 0.7f;
        public float split_threshold = 0.5f;
        public bool rotateHalfSlice = true;
        public float framesForward = 5f;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            bioClouds = GameObject.FindObjectOfType<BioClouds>();

        }
        protected override void OnUpdate()
        {
            //float angleBetweenSums = 0f;
            //float sumMagnitude = 0f;
            //float desired_sumMagnitude = 0;
            for (int i = 0; i < m_CloudGroup.Length; i ++)
            {
                Debug.DrawLine(m_CloudGroup.Position[i].Value,
                    m_CloudGroup.Position[i].Value + m_RightPreference.dessums[i],
                    Color.green);
                Debug.DrawLine(m_CloudGroup.Position[i].Value,
                    m_CloudGroup.Position[i].Value + m_RightPreference.sums[i],
                    Color.yellow);

                if (m_CloudGroup.CloudSplitData[i].splitCount < m_CloudGroup.CloudSplitData[i].CloudSplitLimit &&
                    m_CloudGroup.CloudData[i].AgentQuantity > m_CloudGroup.CloudSplitData[i].CloudSizeLimit)
                {
                    //angleBetweenSums = Vector3.Angle(m_RightPreference.sums[i], m_RightPreference.dessums[i]);
                    //sumMagnitude = math.length(m_RightPreference.sums[i]);
                    //desired_sumMagnitude = math.length(m_RightPreference.dessums[i]);
                    /*Debug.Log("------------");
                    Debug.Log("ID: " + m_CloudGroup.CloudData[i].ID);
                    Debug.Log("Pos: " + m_CloudGroup.Position[i].Value);
                    Debug.Log("Sums: " + m_RightPreference.sums[i]);
                    Debug.Log("DesSums: " + m_RightPreference.dessums[i]);

                    //Check angle between Vectors
                    Debug.Log("Angle: " + angleBetweenSums);
                    Debug.Log("Magnitude: " + sumMagnitude);
                    Debug.Log("Radius: " + m_CloudGroup.CloudData[i].Radius);*/
                    ///if (math.abs(angleBetweenSums) >= angleThreshold ||
                    //   sumMagnitude > m_CloudGroup.CloudData[i].Radius * magnitudeRadiusThreshold ||
                    //   sumMagnitude / desired_sumMagnitude < squishiness_threshold)
                    //    SplitCloud(i);
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
                if (!(cell_map.TryGetValue(cell_id, out int result) && result == m_CloudGroup.CloudData[index].ID) && bioClouds.created_cell_ids.Contains(cell_id))
                {
                    avaiable_current_cells++;

                }
            }

            foreach (int cell_id in future_cells)
            {
                //if avaiable, test if id is different.
                if(!(cell_map.TryGetValue(cell_id, out int result) && result == m_CloudGroup.CloudData[index].ID) && bioClouds.created_cell_ids.Contains(cell_id))
                {
                    avaiable_future_cells++;
                }
            }

            return (avaiable_future_cells / avaiable_current_cells) < split_threshold;
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

                offset.x = math.cos(math.radians(((slice * (i)) + (slice / 2f))));
                offset.y = math.sin(math.radians(((slice * (i)) + (slice / 2f))));
                offset.z = 0f;
                offset *= (m_CloudGroup.CloudData[index].Radius * spawnDistanceFromRadius);
                CloudLateSpawn lateSpawn = new CloudLateSpawn();

                lateSpawn.agentQuantity = math.min(agents_slice, total_agents);
                if (i == divisions)
                    lateSpawn.position = basePosition;
                else
                    lateSpawn.position = basePosition + offset;


                //if (!cell_map.Contains(GridConverter.Position2CellID(lateSpawn.position)))
                //    continue;


                if (i == divisions)
                    lateSpawn.agentQuantity += data.AgentQuantity % (divisions + 1);


                total_agents -= lateSpawn.agentQuantity;
                
                lateSpawn.goal = m_CloudGroup.CloudGoal[index].EndGoal;
                lateSpawn.cloudType = data.Type;
                lateSpawn.preferredDensity = data.PreferredDensity;
                lateSpawn.radiusChangeSpeed = data.RadiusChangeSpeed;
                lateSpawn.splitCount = m_CloudGroup.CloudSplitData[index].splitCount + 1;
                lateSpawn.fatherID = data.ID;
                lateSpawn.radiusMultiplier = radiusMultiplier;

                //if (total_agents != 0 && i == divisions)
                //    lateSpawn.agentQuantity += total_agents;

                bioClouds.cloudLateSpawns.Add(lateSpawn);
            }
            bioClouds.entitiesToDestroy.Add(m_CloudGroup.Entities[index]);
        }
    }
    
}