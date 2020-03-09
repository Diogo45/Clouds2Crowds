using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationConstants : MonoBehaviour {


    public static SimulationConstants instance;

    //The batch size for most of biocrowds jobs, lower size is better but increases overhead
    public int BatchSize = 1;

    //Take ScreenShots of simulation
    public bool ScreenCaptureSimulation = true;
    //TODO: Figure out naming scheme for several prints of simulations

    //BioCrowds Grid Parameters//
    public bool GridActive = true;
    public int GridResolution = 4;

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
