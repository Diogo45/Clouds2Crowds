using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.AI;

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


        public float agentRadius = 1f;
        public float markerRadius = 0.1f;
        public float MarkerDensity = 0.65f;
        public int TerrainX = 50;
        public int TerrainZ = 50;
        public float FramesPerSecond = 32f;


        public bool showMarkers = false;
        public bool showCells = false;

       
      
        
        public SpawnArea[] SpawnAreas = { new SpawnArea{qtd = 50,
                                         goal = new float3{x = 25, y = 0, z = 50},
                                         max = new int3 {x = 15, y = 0, z = 50},
                                         min = new int3 {x = 0, y = 0, z = 0 },
                                         maxSpeed = 1.3f}
                                        };



        public bool NormalLife = false;
        public bool BioCloudsEnabled = false;

        public bool FluidSim = false;
        public string FluidSimPath = "out.bin";

        public bool SpringSystem = false;
        public int2[] SpringConnections = { new int2(1, 2), new int2(4, 3)};
        

        public bool WayPointOn = false;
        public float3[] WayPoints = new float3[]{
            new float3(25,0,25),
            new float3(45,0,25),
            new float3(25,0,45),
            new float3(15,0,25),
            new float3(25,0,15),
        };


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
        public bool ScreenCap = true;
        public static CrowdExperiment experiment = new CrowdExperiment();
        
        public int treeHeight = 4;
        public static bool QuadTreeActive = true; 


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


            string settingsFile = bioCrowdsFolder.FullName + "\\BaseExperiment.json";
            bool basisCase = System.IO.File.Exists(settingsFile);
            //Debug.Log(basisCase + " " + settingsFile);

            if(!basisCase)
                System.IO.File.WriteAllText(settingsFile, JsonUtility.ToJson(experiment, true));
            else
            {
                Debug.Log("Reading Experiment File");
                string file = System.IO.File.ReadAllText(settingsFile);
                experiment = JsonUtility.FromJson<CrowdExperiment>(file);
            }



        }

        private void Update()
        {
            //NavMeshPath path = new NavMeshPath();
            //bool b = NavMesh.CalculatePath(new Vector3(0, 0.25f, 5f), new Vector3(10f, 0.25f, 100f), NavMesh.AllAreas, path);
            //Debug.Log("b " + b + " L:" + path.corners.Length);

            //Camera.main.transform.position = new Vector3(64.5f, 77.3f, 4.1f);


        }

    }
}