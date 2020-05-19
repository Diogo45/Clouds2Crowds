using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ControlVariables : ISettings
{


    public static ControlVariables instance;

    //Global lock for completing biocrowds 
    public bool LockBioCrowds = false;

    public bool SyncWithFluidSimulator = true;

    public int agentQuantity = 0;

    public bool SpawnAgentStructured = true;

    public float waitFor = 0f;

    public float SimThreshold = 150f;

    public int simIndex = 0;

    public bool DrawAgentMarkers = false;

    public float SpawnAgentStructuredIncrement = 1.5f;

    public bool isLockBioCrowds() {
		return this.LockBioCrowds;
	}

	public void setLockBioCrowds(bool LockBioCrowds) {
		this.LockBioCrowds = LockBioCrowds;
	}

	public bool isSyncWithFluidSimulator() {
		return this.SyncWithFluidSimulator;
	}

	public void setSyncWithFluidSimulator(bool SyncWithFluidSimulator) {
		this.SyncWithFluidSimulator = SyncWithFluidSimulator;
	}


    public override void LoadExperimentFromFile()
    {
        throw new System.NotImplementedException();
    }

    public override void SaveExperimentToFile()
    {
        throw new System.NotImplementedException();
    }

    public override void SetExperiment(ISettings exp)
    {
        instance = (ControlVariables)(exp);
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

