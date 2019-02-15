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
    [UpdateBefore(typeof(CellTagSystem))]
    public class ModuleManager : ComponentSystem
    {
        
        [Inject] NormalLifeMarkerSystem normalLifeMarkerSystem;
        [Inject] MarkerSystem markerSystem;
        [Inject] ContadorDeMerda ContadorDeMerda;
        //[Inject] CellTagSystem cellTagSystem;
        [Inject] StressSystem stressSystem;
        [Inject] NormaLifeAgentMovementVectors normaLifeAgentMovementVectors;
        [Inject] AgentMovementVectors agentMovementVectors;

        protected override void OnUpdate()
        {
            
            var modules = Settings.instance;
            if (!modules.NormalLife)
            {
                stressSystem.Enabled = false;
                normaLifeAgentMovementVectors.Enabled = false;
                normalLifeMarkerSystem.Enabled = false;
                ContadorDeMerda.Enabled = false;
                markerSystem.Enabled = true;
                agentMovementVectors.Enabled = true;
            }
            else
            {
                stressSystem.Enabled = true;
                normaLifeAgentMovementVectors.Enabled = true;
                normalLifeMarkerSystem.Enabled = true;
                ContadorDeMerda.Enabled = true;
                markerSystem.Enabled = false;
                agentMovementVectors.Enabled = false;
            }

        }
        
    }
    
}