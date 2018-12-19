using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;


namespace BioCrowds
{

    [System.Serializable]
    public struct SpawnArea
    {
        public int Size;
        public int3 max;
        public int3 min;
    }

    public class Settings : MonoBehaviour
    {
        public bool NormalLife = false;
        
        public static Settings instance;
        public static int TerrainX = 50;
        public static int TerrainZ = 50;
        public int FramesPerSecond = 30;
        public bool showMarkers = false;
        public bool showCells = false;

        
        public List<SpawnArea> SpawnAreas = new List<SpawnArea>();
        public List<Color> Colors = new List<Color>();
        public List<Mesh> Meshes = new List<Mesh>();

        public List<MeshInstanceRenderer> Renderers = new List<MeshInstanceRenderer>();

        public float agentRadius = 1f;
        public float markerRadius = 0.1f;
        public float MarkerDensity = 0.65f;


        public static int BatchSize = 1;
        //Real value is the sum of all groups instantiated in the bootstrap
        public static int agentQuantity = 0;

        


        public void Awake()
        {
            foreach(Color c in Colors)
            {
                Material m = new Material(Shader.Find("Standard"))
                {
                    color = c,
                    enableInstancing = true
                };
                var renderer = new MeshInstanceRenderer()
                {
                    material = m,
                    mesh = Meshes[0]
                };
                Renderers.Add(renderer);
            }

            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);
        }

    }
}