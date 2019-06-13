using UnityEngine;

namespace BioClouds
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;
    using Unity.Collections;
    using System.Collections.Generic;
    using System.IO;
    public class BioCity
    {
        public EntityArchetype AuxinArchetype;
        public EntityArchetype AgentArchetype;
        public EntityArchetype CloudArchetype;
        public EntityArchetype CellArchetype;

        //View
        public EntityArchetype HeatQuadArchetype;

        public EntityManager BioEntityManager;

        public List<MeshInstanceRenderer> CellMeshes;
        public List<MeshInstanceRenderer> AgentMeshes;
        public List<MeshInstanceRenderer> CloudMeshes;

        public Parameters BioParameters;

        public int CloudIDs;
    }


    public class BioClouds : MonoBehaviour
    {
        public static void DeactivateBioclouds()
        {
            World activeWorld = World.Active;
            activeWorld.GetExistingManager<CloudCellDrawLineSystem>().Enabled = false;
            activeWorld.GetExistingManager<CloudCellTagSystem>().Enabled = false;
            activeWorld.GetExistingManager<CellIDMapSystem>().Enabled = false;
            activeWorld.GetExistingManager<CellMarkSystem>().Enabled = false;
            activeWorld.GetExistingManager<CloudCellTotalWeightSystem>().Enabled = false;
            activeWorld.GetExistingManager<CloudHeatMap>().Enabled = false;
            activeWorld.GetExistingManager<CloudMovementVectorSystem>().Enabled = false;
            activeWorld.GetExistingManager<CloudMoveSystem>().Enabled = false;
            activeWorld.GetExistingManager<CloudRadiusUpdate>().Enabled = false;
            activeWorld.GetExistingManager<CloudRightPreferenceSystem>().Enabled = false;
            activeWorld.GetExistingManager<CloudTagDesiredQuantitySystem>().Enabled = false;
            activeWorld.GetExistingManager<Clouds2CrowdsSystem>().Enabled = false;
            activeWorld.GetOrCreateManager<CloudSplitSystem>().Enabled = false;
            //activeWorld.GetExistingManager<ExperimentEndSystem>().Enabled = false;
        }

        //Properties

        BioCity city;
        EntityManager entityManager;
        //End Properties

        Random r = new Random();
        [SerializeField]
        Experiment exp;


        public GameObject background;
        public GameObject collisionMask;
        public GameObject densityQuad;
        public GameObject mainCamera;

        public List<CloudLateSpawn> cloudLateSpawns = new List<CloudLateSpawn>();
        public List<Entity> entitiesToDestroy = new List<Entity>();
        //Methods
        public void Init()
        {

            Parameters inst = Parameters.Instance;
            entityManager = World.Active.GetOrCreateManager<EntityManager>();
            city = new BioCity();
            city.CellMeshes = new List<MeshInstanceRenderer>();
            city.AgentMeshes = new List<MeshInstanceRenderer>();
            city.CloudMeshes = new List<MeshInstanceRenderer>();

            if(!inst.BioCloudsActive){
                DeactivateBioclouds();
                return;
            }


            city.BioEntityManager = entityManager;
            city.BioParameters = Object.FindObjectOfType<Parameters>();

            var folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var bioCloudsFolder = System.IO.Directory.CreateDirectory(folder + "\\VHLAB\\BioClouds");
            var expFolder = System.IO.Directory.CreateDirectory(folder + "\\VHLAB\\BioClouds\\Experiments");



            exp = LoadExperiment(city.BioParameters.ExperimentPath);

            r.InitState((uint)exp.SeedState);

            Debug.Log("domain: " + exp.Domain);

            city.BioParameters.DefaultDomainMinX = exp.Domain.minX; //0
            city.BioParameters.DefaultDomainMinY = exp.Domain.minY; //1
            city.BioParameters.DefaultDomainMaxX = exp.Domain.maxX; //2
            city.BioParameters.DefaultDomainMaxY = exp.Domain.maxY; //3

            densityQuad.transform.position = new Vector3((exp.Domain.minX + exp.Domain.maxX) / 2, (exp.Domain.minY + exp.Domain.maxY) / 2, 5);
            densityQuad.transform.localScale = new Vector3(exp.Domain.maxX - exp.Domain.minX, exp.Domain.maxY - exp.Domain.minY, 1);
            background.transform.position = new Vector3((exp.Domain.minX + exp.Domain.maxX) / 2, (exp.Domain.minY + exp.Domain.maxY) / 2, 10);
            background.transform.localScale = new Vector3(exp.Domain.maxX - exp.Domain.minX, exp.Domain.maxY - exp.Domain.minY, 1);
            collisionMask.transform.position = new Vector3((exp.Domain.minX + exp.Domain.maxX) / 2, (exp.Domain.minY + exp.Domain.maxY) / 2, 10);
            collisionMask.transform.localScale = new Vector3(exp.Domain.maxX - exp.Domain.minX, exp.Domain.maxY - exp.Domain.minY, 1.01f);

            inst.IDToRecord = exp.IDToRecord;

            float z = 0;
            Camera cam = mainCamera.GetComponent<Camera>();
            if (cam.orthographic)
            {
                z = -15;
            }
            else
            {
               z = -((exp.Domain.maxX - exp.Domain.minX) / 2) / math.tan(math.radians(cam.fieldOfView / 2));
            }
            mainCamera.transform.localPosition  = new Vector3((exp.Domain.minX + exp.Domain.maxX) / 2, (exp.Domain.minY + exp.Domain.maxY) / 2, z);

            Parameters.Instance.MaxSimulationFrames = exp.FramesToRecord;

            GridConverter.Width = city.BioParameters.CellWidth;
            GridConverter.SetDomain(city.BioParameters.DefaultDomainMinX,
                           city.BioParameters.DefaultDomainMinY,
                           city.BioParameters.DefaultDomainMaxX,
                           city.BioParameters.DefaultDomainMaxY);


            city.CloudArchetype = city.BioEntityManager.CreateArchetype(typeof(Position),
                                                              typeof(Rotation),
                                                              typeof(CloudData),
                                                              typeof(CloudGoal),
                                                              typeof(CloudMoveStep),
                                                              typeof(SpawnedAgentsCounter),
                                                              typeof(CloudSplitData));

            city.CellArchetype = city.BioEntityManager.CreateArchetype(typeof(Position),
                                                  typeof(Rotation),
                                                  typeof(CellData));



            
            foreach (MeshMaterial m in city.BioParameters.FixedParameters.CloudRendererData)
            {
                city.CloudMeshes.Add(new MeshInstanceRenderer()
                {
                    mesh = m.mesh,
                    material = m.mat
                });
            }

            Texture2D noise = CreateNoiseTexture.GetNoiseTexture(512, 512, 1, new float2(0.0f, 0.0f));
            noise.wrapMode = TextureWrapMode.Mirror;
            Texture2D density = new Texture2D(inst.Rows, inst.Cols);
            density.wrapMode = TextureWrapMode.Clamp;
            density.filterMode = FilterMode.Point;



            CloudHeatMap.DensityRenderer = densityQuad.GetComponent<MeshRenderer>();
            CloudHeatMap.DensityRenderer.material.SetTexture("_DensityTex", density);
            CloudHeatMap.DensityRenderer.material.SetTexture("_NoiseTex", noise);
            CloudHeatMap.DensityRenderer.material.SetInt("_Rows", inst.Rows);
            CloudHeatMap.DensityRenderer.material.SetInt("_Cols", inst.Cols);
            CloudHeatMap.DensityRenderer.material.SetFloat("_CellWidth", inst.CellWidth);
            CloudHeatMap.DensityRenderer.material.SetTexture("_HeatMapScaleTex", inst.GetHeatScaleTexture());

            
            
            if (!city.BioParameters.DrawCloudToMarkerLines)
                World.Active.GetExistingManager<CloudCellDrawLineSystem>().Enabled = false;

            if (!city.BioParameters.EnableRightPreference)
                World.Active.GetExistingManager<CloudRightPreferenceSystem>().Enabled = false;

            // Setup the Collision Mask
            GetComponent<CollisionMaskSettings>().Init();
        }


        // Creates markers on the defined space.
        public void DefineWorld(float [,] domain, int minDist, int maxDist)
        {

        }

        public void CreateCells(float x, float xf, float y, float yf)
        {

            List<float3> cells = new List<float3>();

            for (float i = x; i < xf; i += city.BioParameters.CellWidth)
            {
                for (float j = y; j < yf; j += city.BioParameters.CellWidth)
                {
                    
                    Entity newCell = entityManager.CreateEntity(city.CellArchetype);

                    entityManager.SetComponentData<Position>(newCell, new Position { Value = new float3(i, j, 0f) });
                    entityManager.SetComponentData<Rotation>(newCell, new Rotation { Value = quaternion.identity });
                    entityManager.SetComponentData<CellData>(newCell, new CellData
                    {
                        ID = GridConverter.Position2CellID(new float3(i, j, 0f)),
                        Area = city.BioParameters.CellWidth * city.BioParameters.CellWidth
                    });
                    cells.Add(new float3(i, j, 0f));
                }
            }

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(Parameters.Instance.LogFilePath + "Cells.txt"))
            {
                foreach (float3 cellPos in cells)
                {
                    string line = string.Format("{0:D0};{1:F3};{2:F3};\n",
                GridConverter.Position2CellID(new float3(cellPos.x, cellPos.y, 0f)),
                cellPos.x,
                cellPos.y
                );

                    file.Write(line);


                }
            }


        }

        public float CloudPreferredRadius(int quantity, float prefferedDensity)
        {
            return (float)math.sqrt(quantity / (prefferedDensity * math.PI));
        }

        public float CloudMinRadius(int quantity)
        {
            return CloudPreferredRadius(quantity, Parameters.Instance.MaxColorDensity);
        }

        public void AddCloud(float3 position, int quantity, float3 goal, int cloudType, float preferredDensity, float radiusChangeSpeed, int splitCout = 0, int fatherID = -1, float radiusMultiplier = 1.0f)
        {

            Entity newCloud = entityManager.CreateEntity(city.CloudArchetype);

            float radius = CloudMinRadius(quantity) * radiusMultiplier;

            entityManager.SetComponentData<Position>(newCloud, new Position { Value = position });
            entityManager.SetComponentData<Rotation>(newCloud, new Rotation { Value = quaternion.identity });
            entityManager.SetComponentData<CloudData>(newCloud, new CloudData { ID = city.CloudIDs++, AgentQuantity = quantity, Radius = radius, MaxSpeed = city.BioParameters.CloudSpeed / city.BioParameters.SimulationFramesPerSecond, Type = cloudType, PreferredDensity = preferredDensity,
               MinRadius = CloudMinRadius(quantity), RadiusChangeSpeed = radiusChangeSpeed});
            entityManager.SetComponentData<CloudGoal>(newCloud, new CloudGoal { SubGoal = goal, EndGoal = goal });
            entityManager.SetComponentData<CloudMoveStep>(newCloud, new CloudMoveStep { Delta = float3.zero});
            entityManager.SetComponentData<SpawnedAgentsCounter>(newCloud, new SpawnedAgentsCounter { Quantity = 0 });
            entityManager.SetComponentData<CloudSplitData>(newCloud, new CloudSplitData { splitCount = splitCout, fatherID = fatherID});

            if (Parameters.Instance.RenderClouds)
                entityManager.AddSharedComponentData<MeshInstanceRenderer>(newCloud, city.CloudMeshes[cloudType % city.CloudMeshes.Count]);

        }
        public void DestroyCloud(Entity entity)
        {
            entityManager.DestroyEntity(entity);
        }

        public void SpawnClouds(int quantity, float minx, float maxx, float miny, float maxy, int cloudType, float3 goal, int cloudSize, float preferredDensity, float radiusChangeSpeed)
        {

            float3 minPos = new float3(minx, miny, 0f);
            float3 maxPos = new float3(maxx, maxy, 0f);

            for (int i = 0; i < quantity; i++) {
                
                float3 position = r.NextFloat3(minPos, maxPos);

                AddCloud(position, cloudSize, goal, cloudType, preferredDensity, radiusChangeSpeed);

            }
            
        }

        //End Methods

        //Experiments
        public Experiment LoadExperiment(string exp)
        {
            StreamReader sr = new StreamReader(exp);
            string s = sr.ReadToEnd();
            return JsonUtility.FromJson<Experiment>(s);
        }

        public void StartExperiment()
        {

            for(int i = 0; i < exp.CellRegions.Length; i++)
            {
                //Debug.Log("Make Cells!");
                CreateCells(exp.CellRegions[i].minX,
                            exp.CellRegions[i].maxX,
                            exp.CellRegions[i].minY,
                            exp.CellRegions[i].maxY);
            }

            for (int i = 0; i < exp.AgentTypes.Length; i++)
            {
                SpawnClouds(exp.AgentTypes[i].Quantity,
                            exp.AgentTypes[i].Region.minX,
                            exp.AgentTypes[i].Region.maxX,
                            exp.AgentTypes[i].Region.minY,
                            exp.AgentTypes[i].Region.maxY,
                            exp.AgentTypes[i].Type,
                            new float3(exp.AgentTypes[i].Goal[0], exp.AgentTypes[i].Goal[1], exp.AgentTypes[i].Goal[2]),
                            exp.AgentTypes[i].CloudSize,
                            exp.AgentTypes[i].PreferredDensity,
                            exp.AgentTypes[i].RadiusChangeSpeed);
            }

        }
        //End Experiments

        //Mono Behavior

        public void Start()
        {

            Init();
            StartExperiment();

            

        }

        public void Update()
        {
            if (cloudLateSpawns.Count > 0)
            {
                for (int i = 0; i < cloudLateSpawns.Count; i++)
                {
                    AddCloud(cloudLateSpawns[i].position,
                            cloudLateSpawns[i].agentQuantity,
                            cloudLateSpawns[i].goal,
                            cloudLateSpawns[i].cloudType,
                            cloudLateSpawns[i].preferredDensity,
                            cloudLateSpawns[i].radiusChangeSpeed,
                            cloudLateSpawns[i].splitCount,
                            cloudLateSpawns[i].fatherID,
                            cloudLateSpawns[i].radiusMultiplier);
                }
                cloudLateSpawns.Clear();
            }

            if (entitiesToDestroy.Count > 0)
            {
                for (int i = entitiesToDestroy.Count - 1; i >= 0; i--)
                {
                    entityManager.DestroyEntity(entitiesToDestroy[i]);
                }
                entitiesToDestroy.Clear();
            }
        }


        //End Monobehavior


    }

}

