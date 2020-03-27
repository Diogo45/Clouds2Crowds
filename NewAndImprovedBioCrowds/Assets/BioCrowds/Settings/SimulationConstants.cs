using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimulationConstants : ISettings {


    public static SimulationConstants instance;

    //The batch size for most of biocrowds jobs, lower size is better but increases overhead
    public int BatchSize = 1;

    //Take ScreenShots of simulation
    public bool ScreenCaptureSimulation = true;
    //TODO: Figure out naming scheme for several prints of simulations

    //BioCrowds Grid Parameters//
    public bool GridActive = true;
    public int GridResolution = 4;

    //BioCrowds simulation rate
    public float BioCrowdsTimeStep = 1f / 30f;




	public override void SaveExperimentToFile()
	{
		throw new System.NotImplementedException();
	}

	public override void SetExperiment(ISettings exp)
	{
		instance = (SimulationConstants)(exp);
	}

	public override void LoadExperimentFromFile()
	{
		throw new System.NotImplementedException();
	}



	public int getBatchSize() {
		return this.BatchSize;
	}

	public void setBatchSize(int size) {
		this.BatchSize = size;
	}


	public bool isScreenCaptureSimulation() {
		return this.ScreenCaptureSimulation;
	}

	public void setScreenCaptureSimulation(bool ScreenCaptureSimulation) {
		this.ScreenCaptureSimulation = ScreenCaptureSimulation;
	}

	


	public bool isGridActive() {
		return this.GridActive;
	}

	public void setGridActive(bool GridActive) {
		this.GridActive = GridActive;
	}

	public int getGridResolution() {
		return this.GridResolution;
	}

	public void setGridResolution(int GridResolution) {
		this.GridResolution = GridResolution;
	}


	public float getBioCrowdsTimeStep() {
		return this.BioCrowdsTimeStep;
	}

	public void setBioCrowdsTimeStep(float BioCrowdsTimeStep) {
		this.BioCrowdsTimeStep = BioCrowdsTimeStep;
	}

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    



}
