using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.AI;

//FUTURE: Add a way simulate several scenarios with different parameters
/* We are utilizing Unity's Entity Component System, the documentation is available in https://github.com/Unity-Technologies/EntityComponentSystemSamples/blob/master/Documentation/index.md
 * This is the BootStrap for the BioCrowds Simulator. It's used for creating the Archetypes of every Entity type we'll use in the simulation, that is the Agent Entity, ...
 * Also it'll instantiate these archetypes into the scene with their respective models.
 */
namespace BioCrowds
{

    public class BioCrowdsBootStrap : MonoBehaviour
    {
        public static EntityArchetype AgentArchetype;
        public static EntityArchetype CellArchetype;
        public static EntityArchetype MakerArchetype;

        
        public static MeshInstanceRenderer AgentRenderer;
        public static MeshInstanceRenderer CellRenderer;
        public static MeshInstanceRenderer MarkerRenderer;
        public static Settings BioSettings;



        public static void Start()
        {



            //Getting data from settings
            float agentRadius = CrowdExperiment.instance.agentRadius;

            float framesPerSecond = CrowdExperiment.instance.FramesPerSecond;

            float markerRadius = CrowdExperiment.instance.markerRadius;

            float MarkerDensity = CrowdExperiment.instance.MarkerDensity;

            bool showCells = CrowdExperiment.instance.showCells;

            bool showMarkers = CrowdExperiment.instance.showMarkers;

            int2 size = new int2(CrowdExperiment.instance.TerrainX, CrowdExperiment.instance.TerrainZ);

            if((size.x % 2 !=0 || size.y % 2 != 0))
            {
                Debug.Log("Tamanho do Terreno Invalido");
                return;
            }

            //Just to have a nicer terrain
            //var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //ground.transform.localScale = new Vector3(size.x, 0.5f, size.y);
            //ground.transform.position = ground.transform.localScale / 2;
            //ground.isStatic = true;
            //var navmesh = ground.AddComponent<NavMeshSurface>();
            //navmesh.BuildNavMesh();
            
            

            //The EntityManager is responsible for the creation of all Archetypes, Entities, ... and adding or removing Components from existing Entities 
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();



            float densityToQtd = MarkerDensity / Mathf.Pow(markerRadius, 2f);
            int qtdMarkers = Mathf.FloorToInt(densityToQtd);
            Debug.Log("Marcadores por celula:" + qtdMarkers);

            CellArchetype = entityManager.CreateArchetype(
                ComponentType.Create<CellName>(),
                ComponentType.Create<Position>());

            //The cells are 2x2 so there are (X*Z)/2*2 cells 
            int qtdX = (int)(size.x / (agentRadius * 2));
            int qtdZ = (int)(size.y / (agentRadius * 2));
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

            int qtd = qtdX;

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

            //QuadTree qt = new QuadTree(new Rectangle { x = 0, y = 0, w = size.x, h = size.y }, 0);
            //ShowQuadTree.qt = qt;







            cells.Dispose();

            //Create one entity so the marker spawner injects it and the system runs
            var temp = entityManager.CreateEntity();
            entityManager.AddComponentData(temp, new SpawnData
            {
                qtdPerCell = qtdMarkers
            });
            cellNames.Dispose();



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

}