
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;

//FUTURE: Add a way simulate several scenarios with different parameters
/* We are utilizing Unity's Entity Component System, the documentation is available in https://github.com/Unity-Technologies/EntityComponentSystemSamples/blob/master/Documentation/index.md
 * This is the BootStrap for the BioCrowds Simulator. It's used for creating the Archetypes of every Entity type we'll use in the simulation, that is the Agent Entity, ...
 * Also it'll instantiate these archetypes into the scene with their respective models.
 */
namespace BioCrowds
{

    public struct Group
    {
        public List<GameObject> goals;
        public int qtdAgents;
        public string name;
        public int maxX, minX, maxZ, minZ;
        public float maxSpeed;
        public const float agentRadius = 1f;
    }


   


    public class BioCrowdsBootStrap
    {
        public static EntityArchetype AgentArchetype;
        public static EntityArchetype CellArchetype;
        public static EntityArchetype MakerArchetype;

        public static MeshInstanceRenderer AgentRenderer;
        public static MeshInstanceRenderer CellRenderer;
        public static MeshInstanceRenderer MarkerRenderer;
        public static Settings BioSettings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Intialize()
        {
            //UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

            

            //Get the simulation settings from the settings script
            var gSettings = GameObject.Find("BCSettings");
            BioSettings = gSettings?.GetComponent<Settings>();
            //Getting data from settings
            float agentRadius = BioSettings.agentRadius;

            int framesPerSecond = BioSettings.FramesPerSecond;

            float markerRadius = BioSettings.markerRadius;

            float MarkerDensity = BioSettings.MarkerDensity;

            bool showCells = BioSettings.showCells;

            bool showMarkers = BioSettings.showMarkers;

            

            //FUTURE: BioCrowds in complex 3d models of scenarios
            //TODO: Truncate the Terrain size if the cells don't fit
            var ground = GameObject.Find("Terrain").GetComponent<Terrain>();
            //The EntityManager is responsible for the creation of all Archetypes, Entities, ... and adding or removing Components from existing Entities 
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();



            float densityToQtd = MarkerDensity / Mathf.Pow(markerRadius, 2f);
            int qtdMarkers = Mathf.FloorToInt(densityToQtd);
            Debug.Log("Marcadores por celula:" + qtdMarkers);
            //Here we define the agent archetype by adding all the Components, that is, all the Agent's data. The respective Systems will act upon the Components added, if such Systems exist.
            //REVIEW: See if those are all the necessary Components
            AgentArchetype = entityManager.CreateArchetype(
                ComponentType.Create<Position>(), 
                ComponentType.Create<Rotation>(),
                ComponentType.Create<CellName>(),
                ComponentType.Create<AgentData>(),
                ComponentType.Create<AgentStep>(),
                ComponentType.Create<AgentGoal>(),
                ComponentType.Create<NormalLifeData>(),
                ComponentType.Create<Counter>());

            CellArchetype = entityManager.CreateArchetype(
                ComponentType.Create<CellName>(),
                ComponentType.Create<Position>());


  


            int qtdX = (int)(ground.terrainData.size.x / (agentRadius * 2));
            int qtdZ = (int)(ground.terrainData.size.z / (agentRadius * 2));
            Debug.Log(qtdX + "X" + qtdZ);
            Settings.TerrainX = (int)ground.terrainData.size.x;
            Settings.TerrainZ = (int)ground.terrainData.size.z;
            //For instantiating Entities we first need to create a buffer for all the Enities of the same archetype.
            NativeArray<Entity> cells = new NativeArray<Entity>(qtdX * qtdZ, Allocator.Persistent);
            NativeList<Entity> cellsPersistent = new NativeList<Entity>(qtdX * qtdZ, Allocator.Persistent);

            //Array contaning the cell names, that is, their position on the world
            NativeList<int3> cellNames = new NativeList<int3>(qtdX * qtdZ, Allocator.Persistent);

            //Maping between a cell and their respective markers
            NativeHashMap<int3, NativeSlice<MarkerData>> cellMarkersCache = new NativeHashMap<int3, NativeSlice<MarkerData>>(qtdX * qtdZ, Allocator.Persistent);

            //Creates a Entity of CellArchetype for each basic entity in cells
            entityManager.CreateEntity(CellArchetype, cells);
            //Only gets the MeshRenderer from the Hierarchy
            CellRenderer = GetLookFromPrototype("CellMesh");


            //FUTURE: Make the instantiation easily modifiable
            //Now for each Entity of CellArchetype we define the proper data to the Components int the archetype.  
            for (int i = 0; i < qtdX; i++)
            {
                for (int j = 0; j < qtdZ; j++)
                {

                    float x = i * (agentRadius * 2);
                  
                    float y = 0f;
                    float z = j * (agentRadius * 2);
                    
                    int index = i * qtdX + j;
                    //Debug.Log(index + " " + i + " " + x + " " + z);
                    //entityManager.SetComponentData(cells[index], new Position
                    //{
                    //    Value = new float3(x, y, z)
                    //});


                    entityManager.SetComponentData(cells[index], new CellName
                    {
                        Value = new int3(Mathf.FloorToInt(x) + 1, Mathf.FloorToInt(y), Mathf.FloorToInt(z) + 1)
                    });

                    


                    cellsPersistent.Add(cells[index]);
                    if (showCells)
                        entityManager.AddSharedComponentData(cells[index], CellRenderer);
                    cellNames.Add(entityManager.GetComponentData<CellName>(cells[index]).Value);

                }

            }

            cells.Dispose();
            var temp = entityManager.CreateEntity();
            entityManager.AddComponentData(temp, new SpawnData
            {
                qtdPerCell = qtdMarkers
            });



            //List<Group> groups = new List<Group>();
            //int group = 1;
            //foreach(SpawnArea area in Settings.instance.SpawnAreas)
            //{
            //    List<GameObject> res;
            //    FindGoals(group, out res);
            //    group++;
            //    Group i = new Group
            //    {
            //        goals = res,
            //        //TODO:Hardcode MaxSpeed
            //        maxSpeed = 1.3f,
            //        qtdAgents = area.Size,
            //        maxX = area.max.x,
            //        minX = area.min.x,
            //        maxZ = area.max.z,
            //        minZ = area.min.z

            //    };
            //    Settings.agentQuantity += area.Size;
            //    groups.Add(i);
            //}
            //group = 0;
            //int startID = 0;
            //foreach (Group g in groups)
            //{
            //    int frameRate = Settings.instance.FramesPerSecond;
            //    var renderer = Settings.instance.Renderers[group];
            //    int lastID;
            //    SpawnAgent(frameRate, entityManager, g, startID, out lastID, renderer);
            //    startID = lastID;
            //    group++;
            //}


            cellMarkersCache.Dispose();

            cellNames.Dispose();
            cellsPersistent.Dispose();
            //system.Update();

        }

        //TODO: Parameters according to Group
        public static void SpawnAgent(int framesPerSecond, EntityManager entityManager, Group group,int startID, out int lastId, MeshInstanceRenderer AgentRenderer)
        {
            int doNotFreeze = 0;

            int qtdAgtTotal = group.qtdAgents;
            int maxZ = group.maxZ;
            int maxX = group.maxX;
            int minZ = group.minZ;
            int minX = group.minX;
            float maxSpeed = group.maxSpeed;
            List<GameObject> Goals = group.goals;

            lastId = startID;
            for (int i = startID; i < qtdAgtTotal + startID; i++)
            {
                if (doNotFreeze > qtdAgtTotal)
                {
                    doNotFreeze = 0;
                    maxZ += 2;
                    maxX += 2;
                }

                int CellX = (int)UnityEngine.Random.Range(minX, maxX);
                int CellZ = (int)UnityEngine.Random.Range(minZ, maxZ);
                int CellY = 0;

                while (CellX % 2 == 0 || CellZ % 2 == 0)
                {
                    CellX = (int)UnityEngine.Random.Range(minX, maxX);
                    CellZ = (int)UnityEngine.Random.Range(minZ, maxZ);
                }
                //Debug.Log(x + " " + z);


                float x = CellX;
                float z = CellZ;


                float3 g = Goals[0].transform.position;

                x = UnityEngine.Random.Range(x - 0.99f, x + 0.99f);
                float y = 0f;
                z = UnityEngine.Random.Range(z - 0.99f, z + 0.99f);



                Collider[] hitColliders = Physics.OverlapSphere(new Vector3(x, 0, z), 0.5f);

                //TODO:Check distances between agents
                if (hitColliders.Length > 0)
                {
                    //try again
                    i--;
                    doNotFreeze++;
                    continue;
                }
                else
                {
                    var newAgent = entityManager.CreateEntity(AgentArchetype);
                    entityManager.SetComponentData(newAgent, new Position { Value = new float3(x, y, z) });
                    entityManager.SetComponentData(newAgent, new Rotation { Value = Quaternion.identity });
                    entityManager.SetComponentData(newAgent, new AgentData
                    {
                        ID = i,
                        MaxSpeed = maxSpeed / framesPerSecond,
                        Radius = 1f
                    });
                    entityManager.SetComponentData(newAgent, new AgentStep
                    {
                        delta = float3.zero
                    });
                    entityManager.SetComponentData(newAgent, new Rotation
                    {
                        Value = quaternion.identity
                    });
                    entityManager.SetComponentData(newAgent, new CellName { Value = new int3(CellX, CellY, CellZ) });
                    entityManager.SetComponentData(newAgent, new AgentGoal { SubGoal = g, EndGoal = g });
                    //entityManager.AddComponent(newAgent, ComponentType.FixedArray(typeof(int), qtdMarkers));
                    //TODO:Normal Life stuff change
                    entityManager.SetComponentData(newAgent, new Counter { Value = 0 });
                    entityManager.SetComponentData(newAgent, new NormalLifeData {
                        confort = 0,
                        stress = 0,
                        agtStrAcumulator = 0f,
                        movStrAcumulator = 0f,
                        incStress = 0f
                    });

                     
                    entityManager.AddSharedComponentData(newAgent, AgentRenderer);
                }


                lastId++;
            }
        }

        public static float Distance(float3 X, float3 Y)
        {
            float result = Mathf.Infinity;
            float x1 = X.x, x2 = X.y, x3 = X.z;
            float y1 = Y.x, y2 = Y.y, y3 = Y.z;
            result = Mathf.Sqrt(Mathf.Pow(y1 - x1, 2) + Mathf.Pow(y2 - x2, 2) + Mathf.Pow(y3 - x3, 2));
            return result;
        }

        public static MeshInstanceRenderer GetLookFromPrototype(string protoName)
        {
            var proto = GameObject.Find(protoName);
            if (!proto) Debug.Log("asdasdas");
            var result = proto.GetComponent<MeshInstanceRendererComponent>().Value;
            Object.Destroy(proto);
            return result;
        }

        public static bool FindGoals(int group, out List<GameObject> res)
        {
            int i = 1;
            res = new List<GameObject>();
            GameObject g = GameObject.Find("G-" + group + "-" + i);
            if(!g) return false;
            res.Add(g);
            while (g)
            {
                g = GameObject.Find("G-" + group + "-" + i);
                res.Add(g);
                i++;
            }
            return true;
        }


    }


}
