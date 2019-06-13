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
            public float maxSpeed;
        }


        public bool NormalLife = false;
        public bool BioCloudsEnabled = false;
        public int TerrainX = 50;
        public int TerrainZ = 50;
        public int FramesPerSecond = 30;
        public bool showMarkers = false;
        public bool showCells = false;
        public SpawnArea[] SpawnAreas = { new SpawnArea{qtd = 50,
                                         goal = new float3{x = 25, y = 0, z = 50},
                                         max = new int3 {x = 15, y = 0, z = 50},
                                         min = new int3 {x = 0, y = 0, z = 0 },
                                         maxSpeed = 1.3f},
                                         new SpawnArea{qtd = 50,
                                         goal = new float3{x = 50, y = 0, z = 25},
                                         max = new int3 {x = 15, y = 0, z = 50},
                                         min = new int3 {x = 0, y = 0, z = 0 },
                                         maxSpeed = 2f},
                                         new SpawnArea{qtd = 50,
                                         goal = new float3{x = 25, y = 0, z = 25},
                                         max = new int3 {x = 15, y = 0, z = 50},
                                         min = new int3 {x = 0, y = 0, z = 0 },
                                         maxSpeed = 1.5f},
                                        };
        public float3[] WayPoints = new float3[]{
            new float3(25,0,25),
            new float3(45,0,25),
            new float3(25,0,45),
            new float3(15,0,25),
            new float3(25,0,15),
        };





        public bool WayPointOn = false;
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
        private static CrowdExperiment _experiment;
        public static CrowdExperiment experiment = new CrowdExperiment();//{ get { if (experiment == null) _experiment = new CrowdExperiment(); return _experiment; } }
        
        public int treeHeight = 4;
        public static bool QuadTreeActive = true;

        public string LogPath = @"";

        public void Awake()
        {

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
            var logsFolder = System.IO.Directory.CreateDirectory(folder +  "\\VHLAB\\BioCrowds\\Logs");

            //TODO: FIX THIS PATHING SHIT, LOGPATH

            LogPath = logsFolder.FullName;

            string settingsFile = bioCrowdsFolder.FullName + "\\BaseExperiment.json";
            bool basisCase = System.IO.File.Exists(settingsFile);
            //Debug.Log(basisCase + " " + settingsFile);

            if(!basisCase)
                System.IO.File.WriteAllText(settingsFile, JsonUtility.ToJson(experiment, true));
            else
            {
                string file = System.IO.File.ReadAllText(settingsFile);
                experiment = JsonUtility.FromJson<CrowdExperiment>(file);
            }



        }

    }
}