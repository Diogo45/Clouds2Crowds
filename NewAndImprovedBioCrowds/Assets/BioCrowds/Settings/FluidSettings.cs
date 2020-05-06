using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class FluidSettings : ISettings
{
    [System.Serializable]
    public struct CubeObstacleData
    {
        public float3 position;
        public float3 rotation;

    }


    public static FluidSettings instance;

    
    public bool randTau = false;

  
    public bool randMass = false;

    public float initialTau = 1f;



    public float thresholdDist = 0.01f;
    public float thresholdHeigth = 2f;

    public float particleMass = 0.0001f;//kg

    //public float timeStep = 1f / BioCrowds.Settings.experiment.FramesPerSecond;
    public float particleRadius = 0.25f;//Meters

    public int frameSize = 100000;
    public float personRadius = 0.5f;

	public string customComandLine;

    public float3 scale = new float3(10f, 10f, 10f);
    public float3 translate = new float3(50f, 0f, 25f);

	public bool isRandTau() {
		return this.randTau;
	}

	public void setRandTau(bool randTau) {
		this.randTau = randTau;
	}


	public void setRandMass(bool randMass) {
		this.randMass = randMass;
	}

	public float getInitialTau() {
		return this.initialTau;
	}

	public void setInitialTau(float initialTau) {
		this.initialTau = initialTau;
	}

	public float getThresholdDist() {
		return this.thresholdDist;
	}

	public void setThresholdDist(float thresholdDist) {
		this.thresholdDist = thresholdDist;
	}

	public float getThresholdHeigth() {
		return this.thresholdHeigth;
	}

	public void setThresholdHeigth(float thresholdHeigth) {
		this.thresholdHeigth = thresholdHeigth;
	}

	public float getParticleMass() {
		return this.particleMass;
	}

	public void setParticleMass(float particleMass) {
		this.particleMass = particleMass;
	}

	
	public float getParticleRadius() {
		return this.particleRadius;
	}

	public void setParticleRadius(float particleRadius) {
		this.particleRadius = particleRadius;
	}

	public int getFrameSize() {
		return this.frameSize;
	}

	public void setFrameSize(int frameSize) {
		this.frameSize = frameSize;
	}

	public float getPersonRadius() {
		return this.personRadius;
	}

	public void setPersonRadius(float personRadius) {
		this.personRadius = personRadius;
	}

	public float3 getScale() {
		return this.scale;
	}

	public void setScale(float3 scale) {
		this.scale = scale;
	}

	public float3 getTranslate() {
		return this.translate;
	}

	public void setTranslate(float3 translate) {
		this.translate = translate;
	}




    [SerializeField]
    public CubeObstacleData[] cubeObstacleDatas =
    {
        ////IN METERS
        //new CubeObstacleData
        //{
        //    position = new float3(0f, 0.05f, 0f),
        //    rotation = float3.zero
        //}
    };


    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

		//Initial Value
		//TODO:Make the path of the SplishSplash Simulator automatic detect or choosable in interface
		customComandLine = @"E:\PUCRS\BioSplishSplash\BioSPlisHSPlasH\bin\DynamicBoundarySimulator.exe \..\data\Scenes\Emitter1,5.json";


	}

    public override void SaveExperimentToFile()
    {
        throw new System.NotImplementedException();
    }

    public override void SetExperiment(ISettings exp)
    {
        var newFluidSettings = (FluidSettings)exp;
        instance = newFluidSettings;
    }

    public override void LoadExperimentFromFile()
    {
        throw new System.NotImplementedException();
    }
}
