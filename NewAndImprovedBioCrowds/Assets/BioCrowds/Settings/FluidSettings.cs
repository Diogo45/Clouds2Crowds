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


    public float3 scale = new float3(10f, 10f, 10f);
    public float3 translate = new float3(50f, 0f, 25f);

    public string FluidSimPath = @"D:\BackUp\SPlisHSPlasH\data\Scenes\Emitter.json";




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
