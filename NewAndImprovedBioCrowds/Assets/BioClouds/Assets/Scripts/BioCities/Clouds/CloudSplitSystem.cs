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
    [DisableAutoCreation]
    public class CloudSplitSystem : ComponentSystem
    {
        public struct CloudGroup
        {
            [ReadOnly] public ComponentDataArray<CloudData> CloudData;
            [ReadOnly] public ComponentDataArray<CloudGoal> CloudGoal;
            [ReadOnly] public ComponentDataArray<CloudSplitData> CloudSplitData;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public readonly int Length;
        }
        [Inject] public CloudGroup m_CloudGroup;
        [Inject] public CloudRightPreferenceSystem m_RightPreference;

        public BioClouds bioClouds;

        public int divisions = 6;
        public float spawnDistanceFromRadius = 0.5f;
        public float radiusMultiplier = 3.0f;
        public float angleThreshold = 120.0f;
        public float magnitudeRadiusThreshold = 3.0f;
        public bool rotateHalfSlice = true;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            bioClouds = GameObject.FindObjectOfType<BioClouds>();
        }
        protected override void OnUpdate()
        {
            float angleBetweenSums = 0f;
            float sumMagnitude = 0f;

            for (int i = 0; i < m_CloudGroup.Length; i ++)
            {
                Debug.DrawLine(m_CloudGroup.Position[i].Value,
                    m_CloudGroup.Position[i].Value + m_RightPreference.dessums[i],
                    Color.green);
                Debug.DrawLine(m_CloudGroup.Position[i].Value,
                    m_CloudGroup.Position[i].Value + m_RightPreference.sums[i],
                    Color.yellow);

                if (m_CloudGroup.CloudData[i].ID == 0)
                {
                    angleBetweenSums = Vector3.Angle(m_RightPreference.sums[i], m_RightPreference.dessums[i]);
                    sumMagnitude = new Vector3(m_RightPreference.sums[i].x, 
                                               m_RightPreference.sums[i].y, 
                                               m_RightPreference.sums[i].z).magnitude;
                    /*Debug.Log("------------");
                    Debug.Log("ID: " + m_CloudGroup.CloudData[i].ID);
                    Debug.Log("Pos: " + m_CloudGroup.Position[i].Value);
                    Debug.Log("Sums: " + m_RightPreference.sums[i]);
                    Debug.Log("DesSums: " + m_RightPreference.dessums[i]);

                    //Check angle between Vectors
                    Debug.Log("Angle: " + angleBetweenSums);
                    Debug.Log("Magnitude: " + sumMagnitude);
                    Debug.Log("Radius: " + m_CloudGroup.CloudData[i].Radius);*/
                    if (math.abs(angleBetweenSums) >= angleThreshold &&
                        sumMagnitude > m_CloudGroup.CloudData[i].Radius * magnitudeRadiusThreshold)
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

        private void SplitCloud(int index)
        {
            CloudData data = m_CloudGroup.CloudData[index];
            float3 basePosition = m_CloudGroup.Position[index].Value;
            float3 offset;
            float slice = (360.0f / (float)divisions);
            for (int i = 0; i < divisions; i++)
            {
                offset.x = math.cos(math.radians(((slice * i) + (slice / 2f))));
                offset.y = math.sin(math.radians(((slice * i) + (slice / 2f))));
                offset.z = 0f;
                offset *= (m_CloudGroup.CloudData[0].Radius * spawnDistanceFromRadius);
                CloudLateSpawn lateSpawn = new CloudLateSpawn();
                lateSpawn.position = basePosition + offset;
                lateSpawn.agentQuantity = Mathf.FloorToInt(data.AgentQuantity / 6.0f);
                lateSpawn.goal = m_CloudGroup.CloudGoal[index].EndGoal;
                lateSpawn.cloudType = data.Type;
                lateSpawn.preferredDensity = data.PreferredDensity;
                lateSpawn.radiusChangeSpeed = data.RadiusChangeSpeed;
                lateSpawn.splitCount = m_CloudGroup.CloudSplitData[index].splitCount + 1;
                lateSpawn.fatherID = data.ID;
                lateSpawn.radiusMultiplier = radiusMultiplier;

                bioClouds.cloudLateSpawns.Add(lateSpawn);
            }
            bioClouds.entitiesToDestroy.Add(m_CloudGroup.Entities[index]);
        }
    }
    
}