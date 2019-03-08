using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;


namespace BioCrowds
{
    [Serializable]
    public class CrowdExperiment
    {
        [System.Serializable]
        public struct SpawnArea
        {
            public int qtd;
            public int3 max;
            public int3 min;
            public float3 goal;
        }


        public bool NormalLife = false;
        public bool BioCloudsEnabled = false;
        public static int TerrainX = 50;
        public static int TerrainZ = 50;
        public int FramesPerSecond = 30;
        public bool showMarkers = false;
        public bool showCells = false;
        public SpawnArea[] SpawnAreas = { new SpawnArea{qtd = 50,
                                         goal = new float3{x = 50, y = 0, z = 25},
                                         max = new int3 {x = 15, y = 0, z = 50},
                                         min = new int3 {x = 0, y = 0, z = 0 } } };
        public float agentRadius = 1f;
        public float markerRadius = 0.1f;
        public float MarkerDensity = 0.65f;

    }


    public class Settings : MonoBehaviour
    {
        public static Settings instance;
        public List<Color> Colors = new List<Color>();
        public List<Mesh> Meshes = new List<Mesh>();
        public List<MeshInstanceRenderer> Renderers = new List<MeshInstanceRenderer>();

        public static int BatchSize = 1;
        //Real value is the sum of all groups instantiated in the bootstrap
        public static int agentQuantity = 0;
        public static CrowdExperiment experiment;
        


        public void Awake()
        {
            experiment = new CrowdExperiment();

            foreach (Color c in Colors)
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

            var folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            var bioCrowdsFolder = System.IO.Directory.CreateDirectory(folder +  "\\VHLAB\\BioCrowds");


            string settingsFile = bioCrowdsFolder.FullName + "BaseExperiment.txt";
            bool basisCase = System.IO.File.Exists(settingsFile) ?  true : false;
            Debug.Log(basisCase + " " + settingsFile);


            string exp = JsonUtility.ToJson(experiment);
            System.IO.File.WriteAllText(settingsFile, exp);

       





        }

    }
}