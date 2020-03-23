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
   


    public class Settings : MonoBehaviour
    {
        public static Settings instance;
        


       
        //Real value is the sum of all groups instantiated in the bootstrap



     
        public static string FluidExpName = "Emitter.json";
        //public static int frame = 0;


        private LineRenderer line;


        public void Start()
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
            if (FluidSettings.instance.Enabled)
            {
                DrawSprings();
            }






        }

    }
}