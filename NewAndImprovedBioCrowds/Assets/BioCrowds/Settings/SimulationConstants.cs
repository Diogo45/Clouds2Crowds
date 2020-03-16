using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimulationConstants : ISettings {


    public static SimulationConstants instance;

    //The batch size for most of biocrowds jobs, lower size is better but increases overhead
    [SerializeField]
    public int BatchSize = 1;

    //Take ScreenShots of simulation
    [SerializeField]
    public bool ScreenCaptureSimulation = true;
    //TODO: Figure out naming scheme for several prints of simulations

    //BioCrowds Grid Parameters//
    [SerializeField]
    public bool GridActive = true;
    [SerializeField]
    public int GridResolution = 4;

    //BioCrowds simulation rate
    [SerializeField]
    public float BioCrowdsTimeStep = 1f / 30f;

    public override void SaveExperimentToFile()
    {
        throw new System.NotImplementedException();
    }

    public override void SetExperiment(ISettings exp)
    {
        throw new System.NotImplementedException();
    }

    private void Start()
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
