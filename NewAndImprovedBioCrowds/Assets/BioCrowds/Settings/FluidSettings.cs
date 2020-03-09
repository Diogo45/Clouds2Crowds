using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class FluidSettings : MonoBehaviour
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

    public float springForce = -500f;


    public float thresholdDist = 0.01f;

    public float particleMass = 0.0001f;//kg
    //public float timeStep = 1f / BioCrowds.Settings.experiment.FramesPerSecond;
    public float timeStep = 1f / 30f;
    public float particleRadius = 0.025f;

    //0 --> Total inelastic collision
    //1 --> Elastic Collision
    public float RestitutionCoef = 0f;

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
}
