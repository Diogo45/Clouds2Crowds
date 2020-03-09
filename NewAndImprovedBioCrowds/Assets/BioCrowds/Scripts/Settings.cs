using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
//using UnityEditor.SceneManagement;
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

        public struct ObstacleArea
        {
            public float3 start;
            public float3 end;
        }



        public float agentRadius = 1f;
        public float markerRadius = 0.1f;
        public float MarkerDensity = 0.65f;
        public int TerrainX = 100;
        public int TerrainZ = 50;
        public float FramesPerSecond = 32f;


        public bool showMarkers = false;
        public bool showCells = false;



        public SpawnArea[] SpawnAreas = { new SpawnArea{qtd = 50,
                                         goal = new float3{x = 100, y = 0, z = 25},
                                         max = new int3 {x = 15, y = 0, z = 40},
                                         min = new int3 {x = 0, y = 0, z = 10 },
                                         maxSpeed = 1.3f}
                                        };



        public bool NormalLife = false;
        public bool BioCloudsEnabled = false;

        public bool FluidSim = false;

        public bool SpringSystem = false;
        //public int2[] SpringConnections = { new int2(1, 2), new int2(4, 3) };


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
        public static string ExperimentName = "BaseExperimentFluid.json";


       
        public static int BatchSize = 1;
        //Real value is the sum of all groups instantiated in the bootstrap
        public static int agentQuantity = 0;
        public bool ScreenCap = true;
        public static CrowdExperiment experiment = new CrowdExperiment();

        public int treeHeight = 4;
        public static bool QuadTreeActive = true;
        public static bool SpawnAgentStructured = true;
        public static float waitFor = 0f;


        [SerializeField]
        public List<ISettings> ModuleSettings;

        public static float SimThreshold = 150f;
        public static string FluidExpName = "Emitter.json";
        //public static int frame = 0;
        public static int simIndex = 0;
        //HACK: Change get method
        // This looks ok, to be honest
        //public FluidSettings getFluid() => ((FluidSettings)Settings.instance.ModuleSettings[simIndex]);

        private LineRenderer line;


        public void Awake()
        {
            var args = System.Environment.GetCommandLineArgs();
#if !UNITY_EDITOR
            if (args.Length > 0)
            {
                ExperimentName = args[1];
                simIndex = int.Parse(args[2]);
                FluidExpName = args[3];
            }
#endif
            //line = gameObject.AddComponent<LineRenderer>();
            lineRenderers = new List<LineRenderer>();
            agentsPath = new List<LineRenderer>();

            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);

            var folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            var bioCrowdsFolder = System.IO.Directory.CreateDirectory(folder + "\\VHLAB\\BioCrowds");


            string settingsFile = bioCrowdsFolder.FullName + "\\" + ExperimentName;
            bool basisCase = System.IO.File.Exists(settingsFile);
            //Debug.Log(basisCase + " " + settingsFile);

            if (!basisCase)
                System.IO.File.WriteAllText(settingsFile, JsonUtility.ToJson(experiment, true));
            else
            {
                Debug.Log("Reading Experiment File");
                string file = System.IO.File.ReadAllText(settingsFile);
                experiment = JsonUtility.FromJson<CrowdExperiment>(file);
            }

            var fluidSystem = World.Active.GetOrCreateManager<FluidParticleToCell>();


        }

        public List<LineRenderer> lineRenderers;
        public List<LineRenderer> agentsPath;

        IEnumerator DrawPaths()
        {
            var tagSystem = World.Active.GetOrCreateManager<CellTagSystem>();
            line = gameObject.GetComponent<LineRenderer>();

            if (tagSystem.agentGroup.Length > 0)
            {
                for (int i = 0; i < tagSystem.agentGroup.Length; i++)
                {
                    LineRenderer lineRend;
                    if (i < agentsPath.Count)
                    {
                        lineRend = agentsPath[i];


                    }
                    else
                    {
                        var g = new GameObject("LineRenderer" + i);
                        lineRend = g.AddComponent<LineRenderer>();
                        lineRend.material = line.material;
                        lineRend.SetVertexCount(500);
                        lineRend.SetColors(Color.black, Color.black);
                        lineRend.SetWidth(line.startWidth / 4f, line.endWidth / 5f);
                        lineRend.numCornerVertices = line.numCornerVertices;
                        lineRend.numCapVertices = line.numCapVertices;
                        float3 posInit = tagSystem.agentGroup.AgentPos[i].Value;
                        for (int j = 0; j < lineRend.positionCount; j++)
                        {
                            lineRend.SetPosition(j, posInit + (float3)Vector3.up * 25f);
                        }

                        agentsPath.Add(lineRend);
                    }
                    float3 pos = tagSystem.agentGroup.AgentPos[i].Value;

                    for (int j = lineRend.positionCount - 1; j > 0; j--)
                    {
                        lineRend.SetPosition(j, lineRend.GetPosition(j - 1));
                    }

                    lineRend.SetPosition(0, pos + (float3)Vector3.up * 25f);


                }
            }
            yield return new WaitForSeconds(1500);

        }

        private void DrawSprings()
        {
            var springSystem = World.Active.GetOrCreateManager<SpringSystem>();

            if (!springSystem.Enabled) return;

            line = gameObject.GetComponent<LineRenderer>();

            for (int i = 0; i < springSystem.springs.Length; i++)
            {
                LineRenderer lineI;
                if (i < lineRenderers.Count)
                {
                    lineI = lineRenderers[i];
                }
                else
                {

                    Debug.Log("Line doesnt exist");
                    var g = new GameObject("Line" + i);

                    lineI = g.AddComponent<LineRenderer>();
                    lineRenderers.Add(lineI);
                    lineI.material = line.material;
                    lineI.SetColors(line.startColor, line.endColor);
                    lineI.SetWidth(line.startWidth / 3f, line.endWidth / 3f);

                }

                int ag1 = springSystem.springs[i].ID1;
                int ag2 = springSystem.springs[i].ID2;
                springSystem.AgentPosMap2.TryGetValue(ag1, out float3 pos1);
                springSystem.AgentPosMap2.TryGetValue(ag2, out float3 pos2);

                //TODO: Make the spring decoupling system remove the visualization of those deleted springs
                if ((pos1 + (float3)Vector3.up * 15f).x == lineI.GetPosition(0).x && (pos1 + (float3)Vector3.up * 15f).z == lineI.GetPosition(0).z
                    && (pos2 + (float3)Vector3.up * 15f).x == lineI.GetPosition(1).x && (pos2 + (float3)Vector3.up * 15f).z == lineI.GetPosition(1).z)
                {
                    var g = lineI.gameObject;
                    lineRenderers.Remove(lineI);
                    Destroy(g);
                    continue;
                }


                lineI.SetPosition(0, pos1 + (float3)Vector3.up * 15f);
                lineI.SetPosition(1, pos2 + (float3)Vector3.up * 15f);


            }
        }



        private void Update()
        {



            //TODO:Figure out how to draw paths in front of everything(maybe not obstacles?)
            StartCoroutine(DrawPaths());
            if (experiment.SpringSystem)
            {
                DrawSprings();
            }






        }

    }
}