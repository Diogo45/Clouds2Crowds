using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;


public class VisualizationComponent : MonoBehaviour {


    VisualizationSystem VisualizationSystem;
    public Dictionary<int,VisualAgent> agentList;
    public List<GameObject> agentPrefabs;
    int previousFrame = -1;


	// Use this for initialization
	void Start ()
    {
        World activeWorld = World.Active;
        VisualizationSystem = activeWorld.GetExistingManager<VisualizationSystem>();
        agentList = new Dictionary<int, VisualAgent>();

        //Debug.Log("VisualizationComponent initialized");
    }
	
	// Update is called once per frame
	void Update () {
        //Debug.Log(VisualizationSystem.CurrentAgentPositions);

        int currentFrame = VisualizationSystem.CurrentFrame;

        if (currentFrame == previousFrame) return;
        
        foreach (VisualizationSystem.AgentRecord ar in VisualizationSystem.CurrentAgentPositions)
        {
            Debug.Log(ar.ToString());
            if (agentList.ContainsKey(ar.AgentID))
            {   
                agentList[ar.AgentID].CurrPosition = (ar.Position);
                Debug.Log("updating" + ar.AgentID + "'s position");
            }
            else
            {
                //pega um prefab pra instanciar,  todos prefabs de uma cloud são iguais ?
                //var agnt = Instantiate(agentPrefabs[(int)Random.Range(0, agentPrefabs.Count)]);
                var agnt = Instantiate(agentPrefabs[0]);
                //Debug.Log("instanciando agente");

                //inicializa componente visual com a posição atual
                var va = agnt.GetComponent<VisualAgent>();


                Debug.Log(va == null);

                agentList.Add(ar.AgentID, va);
                va.Initialize(ar.Position);
                va.CurrPosition = (ar.Position);
                va.force = Vector3.zero;
            }

        }

        previousFrame = currentFrame;

	}
}
