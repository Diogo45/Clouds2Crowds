using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class ParticleSystemVisualization : MonoBehaviour {

    ParticleSystem particle_system;
    BioCrowds.FluidParticleToCell fluidSimulation;

    ParticleSystem.Particle[] particles;
    public float size = 0.25f;
    private void Start()
    {
        if (!BioCrowds.Settings.experiment.FluidSim)
        {
            this.enabled = false;
        }
        particle_system = GetComponent<ParticleSystem>();

        fluidSimulation = World.Active.GetOrCreateManager<BioCrowds.FluidParticleToCell>();

        particles = new ParticleSystem.Particle[fluidSimulation.frameSize * 10];

    }


    public void Update()
    {
        UpdateParticles();
    }

    private void UpdateParticles()
    {
        
        var positions =  fluidSimulation.FluidPos;
        if (!positions.IsCreated)
        {
            return;
        }
        for (int i = 0; i < positions.Length; i++)
        {
            
            particles[i].position = positions[i];
            particles[i].startSize = size;

        }

        particle_system.SetParticles(particles, positions.Length);


    }


}
