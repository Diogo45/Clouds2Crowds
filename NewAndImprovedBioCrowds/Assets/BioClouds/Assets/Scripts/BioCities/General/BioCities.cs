using UnityEngine;

namespace BioCities
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

        public EntityArchetype RelationHelper;
        public EntityManager BioEntityManager;

        public List<MeshInstanceRenderer> CellMeshes;
        public List<MeshInstanceRenderer> AgentMeshes;
        public List<MeshInstanceRenderer> CloudMeshes;
        public List<MeshInstanceRenderer> HeatQuadMeshes;

        public Parameters BioParameters;

        public int CloudIDs;
    }


    public class BioCities : MonoBehaviour
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
            activeWorld.GetExistingManager<CloudRadiusUpdateMinMax>().Enabled = false;
            activeWorld.GetExistingManager<CloudRadiusUpdateSpeed>().Enabled = false;
            activeWorld.GetExistingManager<CloudRadiusUpdateTCC>().Enabled = false;
            activeWorld.GetExistingManager<CloudSplitSystem>().Enabled = false;
            activeWorld.GetExistingManager<CloudTagDesiredQuantitySystem>().Enabled = false;
            
            activeWorld.GetExistingManager<ExperimentEndSystem>().Enabled = false;
        }

        //Properties

        BioCity city;
        EntityManager entityManager;
        //End Properties

        Random r = new Random();
        [SerializeField]
        Experiment exp;

        //Methods
        public void Init()
        {

            Parameters inst = Parameters.Instance;
            entityManager = World.Active.GetOrCreateManager<EntityManager>();
            city = new BioCity();
            city.CellMeshes = new List<MeshInstanceRenderer>();
            city.AgentMeshes = new List<MeshInstanceRenderer>();
            city.CloudMeshes = new List<MeshInstanceRenderer>();
            city.HeatQuadMeshes = new List<MeshInstanceRenderer>();

            if(!inst.BioCloudsActive){
                DeactivateBioclouds();
                return;
            }


            city.BioEntityManager = entityManager;
            city.BioParameters = Object.FindObjectOfType<Parameters>();

            exp = LoadExperiment(city.BioParameters.ExperimentPath);
            r.InitState((uint)exp.SeedState);

            city.BioParameters.DomainMinX = exp.Domain[0];
            city.BioParameters.DomainMinY = exp.Domain[1];
            city.BioParameters.DomainMaxX = exp.Domain[2];
            city.BioParameters.DomainMaxY = exp.Domain[3];

            GridConverter.Width = city.BioParameters.CellWidth;
            GridConverter.SetDomain(city.BioParameters.DomainMinX,
                           city.BioParameters.DomainMinY,
                           city.BioParameters.DomainMaxX,
                           city.BioParameters.DomainMaxY);


            city.CloudArchetype = city.BioEntityManager.CreateArchetype(typeof(Position),
                                                              typeof(Rotation),
                                                              typeof(CloudData),
                                                              typeof(CloudGoal),
                                                              typeof(CloudMoveStep));

            city.CellArchetype = city.BioEntityManager.CreateArchetype(typeof(Position),
                                                  typeof(Rotation),
                                                  typeof(CellData));


            city.HeatQuadArchetype = city.BioEntityManager.CreateArchetype(typeof(Position), typeof(HeatQuad));

            foreach(MeshMaterial m in city.BioParameters.CellRendererData)
            {
                city.CellMeshes.Add(new MeshInstanceRenderer()
                {
                    mesh = m.mesh,
                    material = m.mat
                });
            }
            
            foreach (MeshMaterial m in city.BioParameters.CloudRendererData)
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
            

            inst.HeatMapTexture = Parameters.GetHeatScaleTexture(inst.HeatMapColors, inst.HeatMapScaleSize);
            foreach (MeshMaterial m in city.BioParameters.HeatQuadRendererData)
            {

                m.mat.SetTexture("_DensityTex", density);
                m.mat.SetTexture("_NoiseTex", noise);
                m.mat.SetInt("_Rows", inst.Rows);
                m.mat.SetInt("_Cols", inst.Cols);
                m.mat.SetFloat("_CellWidth", inst.CellWidth);
                m.mat.SetTexture("_HeatMapScaleTex", inst.HeatMapTexture);
                city.HeatQuadMeshes.Add(new MeshInstanceRenderer()
                {
                    mesh = m.mesh,
                    material = m.mat
                });
            }

            //Heatmaptexture
            inst.heattextquad.GetComponent<MeshRenderer>().material.SetTexture("_MainTex",inst.HeatMapTexture);
            //end heatmaptexture



            Entity newQuad;
            newQuad = entityManager.CreateEntity(city.HeatQuadArchetype);
            entityManager.SetComponentData<Position>(newQuad, new Position { Value = new float3(-1f, -1f, 0f) });
            entityManager.AddSharedComponentData<MeshInstanceRenderer>(newQuad, city.HeatQuadMeshes[0]);

            //newQuad = entityManager.CreateEntity(city.HeatQuadArchetype);
            //entityManager.SetComponentData<Position>(newQuad, new Position { Value = new float3(-1f, -1f, 0f) });
            //entityManager.AddSharedComponentData<MeshInstanceRenderer>(newQuad, city.HeatQuadMeshes[1]);

            if (!city.BioParameters.DrawCloudToMarkerLines)
                World.Active.GetExistingManager<CloudCellDrawLineSystem>().Enabled = false;

            if (!city.BioParameters.enableCloudSplitSystem)
                World.Active.GetExistingManager<CloudSplitSystem>().Enabled = false;
        }


        // Creates markers on the defined space.
        public void DefineWorld(float [,] domain, int minDist, int maxDist)
        {

        }

        public void CreateCells(float x, float xf, float y, float yf, int cellType)
        {

            List<float3> cells = new List<float3>();

            for(float i = x; i < xf; i+= city.BioParameters.CellWidth)
            {
                for (float j = y; j < yf; j+=city.BioParameters.CellWidth)
                {
                    Entity newCell = entityManager.CreateEntity(city.CellArchetype);

                    entityManager.SetComponentData<Position>(newCell, new Position { Value = new float3(i, j , 0f) });
                    entityManager.SetComponentData<Rotation>(newCell, new Rotation { Value = quaternion.identity });
                    entityManager.SetComponentData<CellData>(newCell, new CellData { ID = GridConverter.Position2CellID(new float3(i, j, 0f)),
                                                                                     Area = city.BioParameters.CellWidth * city.BioParameters.CellWidth,
                                                                                     owningCloud = -1 });
                    cells.Add(new float3(i, j, 0f));

                    //entityManager.AddSharedComponentData<MeshInstanceRenderer>(newCell, city.CellMeshes[cellType]);
                }
            }

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(Parameters.Instance.LogFile + "Cells.txt"))
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
            return math.max(CloudPreferredRadius(quantity, 10), Parameters.Instance.CloudMinRadius);
        }

        public void AddCloud(float3 position, int quantity, float3 goal, int cloudType, float preferredDensity, float radiusChangeSpeed)
        {

            Entity newCloud = entityManager.CreateEntity(city.CloudArchetype);

            float radius = CloudMinRadius(quantity);//, preferredDensity);

            entityManager.SetComponentData<Position>(newCloud, new Position { Value = position });
            entityManager.SetComponentData<Rotation>(newCloud, new Rotation { Value = quaternion.identity });
            entityManager.SetComponentData<CloudData>(newCloud, new CloudData { ID = city.CloudIDs++, AgentQuantity = quantity, Radius = radius, MaxSpeed = city.BioParameters.CloudSpeed / city.BioParameters.SimulationFramesPerSecond, Type = cloudType, PreferredDensity = preferredDensity,
               MinRadius = CloudMinRadius(quantity), RadiusChangeSpeed = radiusChangeSpeed});
            entityManager.SetComponentData<CloudGoal>(newCloud, new CloudGoal { SubGoal = goal, EndGoal = goal });
            entityManager.SetComponentData<CloudMoveStep>(newCloud, new CloudMoveStep { Delta = float3.zero});
            entityManager.AddSharedComponentData<MeshInstanceRenderer>(newCloud, city.CloudMeshes[cloudType]);

        }

        public void SpawnClouds(int quantity, float minx, float maxx, float miny, float maxy, int cloudType, float3 goal, int cloudSize, float preferredDensity, float radiusChangeSpeed)
        {

            float3 minPos = new float3(minx, miny, 0f);
            float3 maxPos = new float3(maxx, maxy, 0f);

            for (int i = 0; i < quantity; i++) {


                //float3 goal = r.NextFloat3(minPos, maxPos);
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
            World.Active.GetExistingManager<CloudRadiusUpdateTCC>().Enabled = false;
            World.Active.GetExistingManager<CloudRadiusUpdateSpeed>().Enabled = false;
            World.Active.GetExistingManager<CloudRadiusUpdateMinMax>().Enabled = false;
            switch (exp.RadiusUpdateType)
            {
                case "TCC":
                    World.Active.GetExistingManager<CloudRadiusUpdateTCC>().Enabled = true;
                    break;
                case "Speed":
                    World.Active.GetExistingManager<CloudRadiusUpdateSpeed>().Enabled = true;
                    break;
                case "DynamicMinMax":

                    World.Active.GetExistingManager<CloudRadiusUpdateMinMax>().Enabled = true;
                    break;
                default:
                    break;
            }

            for(int i = 0; i < exp.CellRegions.Length; i++)
            {
                CreateCells(exp.CellRegions[i].minX, exp.CellRegions[i].minY,
                        exp.CellRegions[i].maxX, exp.CellRegions[i].maxY, 0);
            }

            for (int i = 0; i < exp.AgentTypes.Length; i++)
            {
                SpawnClouds(exp.AgentTypes[i].Quantity,
                            exp.AgentTypes[i].Region[0],
                            exp.AgentTypes[i].Region[2],
                            exp.AgentTypes[i].Region[1],
                            exp.AgentTypes[i].Region[3],
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


        //End Monobehavior


    }

}

