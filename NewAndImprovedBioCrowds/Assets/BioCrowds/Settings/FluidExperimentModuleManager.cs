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
    public class FluidExperimentModuleManager : IModuleManager
    {

        public static FluidExperimentModuleManager instance;

        private AgentMassMapSystem agentMassMapSystem;
        private FluidInitializationSystem fluidInitializationSystem;
        private FluidParticleToCell fluidParticleToCell;
        private FluidMovementOnAgent fluidMovementOnAgent;


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

            agentMassMapSystem = world.GetExistingManager<AgentMassMapSystem>();

            fluidInitializationSystem = world.GetExistingManager<FluidInitializationSystem>();
            fluidParticleToCell = world.GetExistingManager<FluidParticleToCell>();
            fluidMovementOnAgent = world.GetExistingManager<FluidMovementOnAgent>();
           
            Disable();

        }


        public override void Disable()
        {
            agentMassMapSystem.Enabled = false;
            fluidInitializationSystem.Enabled = false;
            fluidParticleToCell.Enabled = false;
            fluidMovementOnAgent.Enabled = false;
            
            
        }

        public override void Enable()
        {
            agentMassMapSystem.Enabled = true;
            fluidInitializationSystem.Enabled = true;
            fluidParticleToCell.Enabled = true;
            fluidMovementOnAgent.Enabled = true;
          
            Created = true;
        }





    }
}
