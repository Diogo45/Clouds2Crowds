using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ControlVariables : ISettings
{


    public static ControlVariables instance;

    //Global lock for completing biocrowds 
    [HideInInspector]
    public bool LockBioCrowds = false;

    public bool SyncWithFluidSimulator = false;

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

