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
        [Inject] MarkerCounter markerCounter;
        [Inject] StressSystem stressSystem;
        [Inject] NormaLifeAgentMovementVectors normaLifeAgentMovementVectors;
        [Inject] AgentMovementVectors agentMovementVectors;
        [Inject] AgentDespawner despawner;

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



        }
        
    }
    
}