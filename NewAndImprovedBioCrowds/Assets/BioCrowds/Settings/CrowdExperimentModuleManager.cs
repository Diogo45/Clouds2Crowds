﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace BioCrowds
{
    public class CrowdExperimentModuleManager : IModuleManager
    {

        private MarkerSpawnSystem markerSpawnSystem;
        private AgentSpawner agentSpawner;
        private CellTagSystem cellTagSystem;
        private MarkerSystemMk2 markerSystemMk2;
        private MarkerSystemView markerSystemView;
        private MarkerWeightSystem markerWeightSystem;
        private AgentMovementVectors agentMovementVectors;
        private AgentMovementSystem agentMovementSystem;

        public override void Disable()
        {
            if (!Created)
            {
                Debug.LogError("Crowd Experiment Systems were not Created");
                return;
            }
            else
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
        }

        public override void Enable()
        {
            if (!Created)
            {
                Debug.Log("RODANDO");

                var world = World.Active;
                
                markerSpawnSystem = world.CreateManager<MarkerSpawnSystem>();

                agentSpawner = world.CreateManager<AgentSpawner>();
                cellTagSystem = world.CreateManager<CellTagSystem>();
                markerSystemMk2 = world.CreateManager<MarkerSystemMk2>();
                markerSystemView = world.CreateManager<MarkerSystemView>();
                markerWeightSystem = world.CreateManager<MarkerWeightSystem>();
                agentMovementVectors = world.CreateManager<AgentMovementVectors>();
                agentMovementSystem = world.CreateManager<AgentMovementSystem>();
                Created = true;
            }
            markerSpawnSystem.Enabled = true;
            agentSpawner.Enabled = true;
            cellTagSystem.Enabled = true;
            markerSystemMk2.Enabled = true;
            markerSystemView.Enabled = true;
            markerWeightSystem.Enabled = true;
            agentMovementVectors.Enabled = true;
            agentMovementSystem.Enabled = true;

        }
    }
}
