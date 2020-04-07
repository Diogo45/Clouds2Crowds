using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace BioCrowds
{
    public class CrowdExperimentModuleManager : IModuleManager
    {

        public static CrowdExperimentModuleManager instance;

        private MarkerSpawnSystem markerSpawnSystem;
        private AgentSpawner agentSpawner;
        private CellTagSystem cellTagSystem;
        private MarkerSystemMk2 markerSystemMk2;
        private MarkerSystemView markerSystemView;
        private MarkerWeightSystem markerWeightSystem;
        private AgentMovementVectors agentMovementVectors;
        private AgentMovementSystem agentMovementSystem;

        public void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);

            var world = World.Active;

            markerSpawnSystem = world.GetExistingManager<MarkerSpawnSystem>();

            agentSpawner = world.GetExistingManager<AgentSpawner>();
            cellTagSystem = world.GetExistingManager<CellTagSystem>();
            markerSystemMk2 = world.GetExistingManager<MarkerSystemMk2>();
            markerSystemView = world.GetExistingManager<MarkerSystemView>();
            markerWeightSystem = world.GetExistingManager<MarkerWeightSystem>();
            agentMovementVectors = world.GetExistingManager<AgentMovementVectors>();
            agentMovementSystem = world.GetExistingManager<AgentMovementSystem>();

            Disable();

        }


        public override void Disable()
        {
            markerSpawnSystem.Enabled = false;
            agentSpawner.Enabled = false;
            cellTagSystem.Enabled = false;
            markerSystemMk2.Enabled = false;
            markerSystemView.Enabled = false;
            markerWeightSystem.Enabled = false;
            agentMovementVectors.Enabled = false;
            agentMovementSystem.Enabled = false;
        }

        public override void Enable()
        {
            markerSpawnSystem.Enabled = true;
            agentSpawner.Enabled = true;
            cellTagSystem.Enabled = true;
            markerSystemMk2.Enabled = true;
            markerSystemView.Enabled = true;
            markerWeightSystem.Enabled = true;
            agentMovementVectors.Enabled = true;
            agentMovementSystem.Enabled = true;

        }


#if UNITY_EDITOR
        [Inject] CellTagSystem CellTagSystem;
        //public void OnDrawGizmos()
        //{
        //    if (!CellTagSystem.Enabled) { return; }
        //    var agentData = CellTagSystem.agentGroup.AgentData;
        //    var agentPos = CellTagSystem.agentGroup.AgentPos;
        //    for (int i = 0; i < agentData.Length; i++)
        //    {
        //        Handles.Label(agentPos[i].Value + (float3)Vector3.up, agentData[i].ID.ToString());
        //    }

        //}
#endif



    }
}
