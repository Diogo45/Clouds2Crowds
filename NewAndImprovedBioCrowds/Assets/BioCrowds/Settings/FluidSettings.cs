using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu()]
[System.Serializable]
public class FluidSettings : ISettings
{
    [System.Serializable]
    public struct CubeObstacleData
    {
        public float3 position;
        public float3 rotation;

    }


    public float thresholdDist = 0.01f;
    //1 g/cm3 = 1000 kg/m3
    //Calculate based on the original SplishSplash code, mass = volume * density
    //Where density = 1000kg/m^3 and volume = 0.8 * particleDiameter^3
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
}
