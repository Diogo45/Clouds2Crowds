using System;
using System.Collections.Generic;
using Unity.Mathematics;
//using UnityEditor.SceneManagement;
using UnityEngine;


public class InteractionSettings : ISettings
{

    public static InteractionSettings instance;

    public float interactionDistance = 1f;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

    }


    //N precisa implementa agr
    public override void LoadExperimentFromFile()
    {
        throw new NotImplementedException();
    }

    //N precisa implementa agr
    public override void SaveExperimentToFile()
    {
        throw new NotImplementedException();
    }

    public override void SetExperiment(ISettings exp)
    {
        instance = (InteractionSettings)(exp);
    }
}