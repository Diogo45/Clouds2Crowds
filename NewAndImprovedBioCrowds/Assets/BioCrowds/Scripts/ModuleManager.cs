using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;
using Unity.Transforms;
using Unity.Jobs;
using UnityEditor;



namespace BioCrowds
{
    /// <summary>
    /// Controls witch systems are active based on the modules of BioCrowds.
    /// </summary>
    [UpdateBefore(typeof(CellTagSystem))]
    public class ModuleManager : ComponentSystem
    {
        
        [Inject] NormalLifeMarkerSystem normalLifeMarkerSystem;
        [Inject] MarkerSystem markerSystem;
        [Inject] MarkerSystemMk2 markerSystemQuadTree;
        [Inject] MarkerCounter markerCounter;
        [Inject] StressSystem stressSystem;
        [Inject] NormaLifeAgentMovementVectors normaLifeAgentMovementVectors;
        [Inject] AgentMovementVectors agentMovementVectors;
        [Inject] AgentDespawner despawner;

        World activeWorld;

        protected override void OnStartRunning()
        {
            Debug.Log(TimeMachineSettings.experiment.Enabled);
            if (TimeMachineSettings.experiment.Enabled)
            {
                Debug.Log("TimeMachine On");
                SetupTimeMachine();
            }
            

        }

        protected override void OnUpdate()
        {
            
            var modules = Settings.experiment;
            if (!modules.NormalLife)
            {
                stressSystem.Enabled = false;
                normaLifeAgentMovementVectors.Enabled = false;
                normalLifeMarkerSystem.Enabled = false;
                markerCounter.Enabled = false;
                markerSystem.Enabled = true;
                agentMovementVectors.Enabled = true;
            }
            else
            {
                stressSystem.Enabled = true;
                normaLifeAgentMovementVectors.Enabled = true;
                normalLifeMarkerSystem.Enabled = true;
                markerCounter.Enabled = true;
                markerSystem.Enabled = false;
                agentMovementVectors.Enabled = false;
            }

            if (Settings.QuadTreeActive)
            {
                markerSystem.Enabled = false;
                markerSystemQuadTree.Enabled = true;
            }
            else
            {
                markerSystem.Enabled = true;
                markerSystemQuadTree.Enabled = false;
            }

            if (!modules.BioCloudsEnabled)
                BioClouds.BioClouds.DeactivateBioclouds();

            //TODO: Time Machine not in Scene
            //if (!TimeMachineSettings.experiment.Enabled && World.Active.GetExistingManager<AgentMovementTimeMachine>().Enabled)
            //    DisableTimeMachine();
        }

        public void SetupTimeMachine()
        {
            World activeWorld = World.Active;
            var manager = activeWorld.GetOrCreateManager<AgentMovementTimeMachine>();
            //manager.LoadDensityValues();
            manager.Enabled = true;
        }

        public void EnableTimeMachine()
        {
            World activeWorld = World.Active;
            activeWorld.GetExistingManager<AgentMovementTimeMachine>().Enabled = true;
        }

        public void DisableTimeMachine()
        {
            World activeWorld = World.Active;
            activeWorld.GetExistingManager<AgentMovementTimeMachine>().Enabled = false;
        }

    }
    
}