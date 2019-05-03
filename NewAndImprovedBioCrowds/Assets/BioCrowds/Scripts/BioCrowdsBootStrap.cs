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
    [System.Obsolete("Contains the data for the deprecated agent spawn method")]
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



            //Getting data from settings
            float agentRadius = Settings.experiment.agentRadius;

            int framesPerSecond = Settings.experiment.FramesPerSecond;

            float markerRadius = Settings.experiment.markerRadius;

            float MarkerDensity = Settings.experiment.MarkerDensity;

            bool showCells = Settings.experiment.showCells;

            bool showMarkers = Settings.experiment.showMarkers;

            int2 size = new int2(Settings.experiment.TerrainX, Settings.experiment.TerrainZ);

            if((size.x % 2 !=0 || size.y % 2 != 0))
            {
                Debug.Log("Tamanho do Terreno Invalido");
                return;
            }

            //Just to have a nicer terrain
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.localScale = new Vector3(size.x, 0.5f, size.y);
            ground.transform.position = ground.transform.localScale / 2; 

            //The EntityManager is responsible for the creation of all Archetypes, Entities, ... and adding or removing Components from existing Entities 
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();



            float densityToQtd = MarkerDensity / Mathf.Pow(markerRadius, 2f);
            int qtdMarkers = Mathf.FloorToInt(densityToQtd);
            Debug.Log("Marcadores por celula:" + qtdMarkers);

            CellArchetype = entityManager.CreateArchetype(
                ComponentType.Create<CellName>(),
                ComponentType.Create<Position>());

            //The cells are 2x2 so there are (X*Z)/2*2 cells 
            int qtdX = (int)(ground.transform.localScale.x / (agentRadius * 2));
            int qtdZ = (int)(ground.transform.localScale.z / (agentRadius * 2));
            Debug.Log(qtdX + "X" + qtdZ);

            //For instantiating Entities we first need to create a buffer for all the Enities of the same archetype.
            NativeArray<Entity> cells = new NativeArray<Entity>(qtdX * qtdZ, Allocator.Persistent);

            //Array contaning the cell names, that is, their position on the world
            NativeList<int3> cellNames = new NativeList<int3>(qtdX * qtdZ, Allocator.Persistent);

            //Creates a Entity of CellArchetype for each basic entity in cells
            entityManager.CreateEntity(CellArchetype, cells);
            //Only gets the MeshRenderer from the Hierarchy
            CellRenderer = GetLookFromPrototype("CellMesh");


            //Now for each Entity of CellArchetype we define the proper data to the Components int the archetype.  

            int qtd = math.max(qtdX, qtdZ);

            for (int i = 0; i < qtdX; i++)
            {
                for (int j = 0; j < qtdZ; j++)
                {

                    float x = i * (agentRadius * 2);

                    float y = 0f;
                    float z = j * (agentRadius * 2);

                    int index = j * qtd + i;

                    entityManager.SetComponentData(cells[index], new Position
                    {
                        Value = new float3(x, y, z)
                    });


                    entityManager.SetComponentData(cells[index], new CellName
                    {
                        Value = new int3(Mathf.FloorToInt(x) + 1, Mathf.FloorToInt(y), Mathf.FloorToInt(z) + 1)
                    });

                    if (showCells) entityManager.AddSharedComponentData(cells[index], CellRenderer);

                    cellNames.Add(entityManager.GetComponentData<CellName>(cells[index]).Value);

                }

            }


            cells.Dispose();

            //Create one entity so the marker spawner injects it and the system runs
            var temp = entityManager.CreateEntity();
            entityManager.AddComponentData(temp, new SpawnData
            {
                qtdPerCell = qtdMarkers
            });
            cellNames.Dispose();

            QuadTree qt = new QuadTree(new Rectangle { x = 0, y = 0, w = size.x, h = size.y }, 0);


        }

        [System.Obsolete("This method is deprecated, use the AgentSpawn system")]
        public static void SpawnAgent(int framesPerSecond, EntityManager entityManager, Group group, int startID, out int lastId, MeshInstanceRenderer AgentRenderer)
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
                    entityManager.SetComponentData(newAgent, new NormalLifeData
                    {
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


        public static MeshInstanceRenderer GetLookFromPrototype(string protoName)
        {
            var proto = GameObject.Find(protoName);
            if (!proto) Debug.Log("asdasdas");
            var result = proto.GetComponent<MeshInstanceRendererComponent>().Value;
            Object.Destroy(proto);
            return result;
        }

        [System.Obsolete("This method is deprecated, define goals by the experiment file")]
        public static bool FindGoals(int group, out List<GameObject> res)
        {
            int i = 1;
            res = new List<GameObject>();
            GameObject g = GameObject.Find("G-" + group + "-" + i);
            if (!g) return false;
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