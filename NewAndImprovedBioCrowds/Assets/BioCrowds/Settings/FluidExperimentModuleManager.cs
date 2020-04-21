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

        private System.Diagnostics.Process SplishSplash;

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

            if(Created)
                SplishSplash.Kill();
        } 

        public override void Enable()
        {
            agentMassMapSystem.Enabled = true;
            fluidInitializationSystem.Enabled = true;
            fluidParticleToCell.Enabled = true;
            fluidMovementOnAgent.Enabled = true;


            SplishSplash = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C " + FluidSettings.instance.customComandLine;
            SplishSplash.StartInfo = startInfo;
            SplishSplash.Start();
            Created = true;
        }





    }
}
