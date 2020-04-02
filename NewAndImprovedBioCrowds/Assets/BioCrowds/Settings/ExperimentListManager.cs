using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TMPro;
using System;
using Unity.Mathematics;

public class ExperimentListManager : MonoBehaviour
{
    public static ExperimentListManager instance;

    public GameObject buttonPrefab;

    private RectTransform myTransform;




    public ParameterDictonary parameterNameToInputField;

    public void Start()
    {

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        myTransform = gameObject.GetComponent<RectTransform>();

        ShowVariablesInputField();
    }


    public void AddExperiment(ExperimentManager.Experiment exp, int index)
    {
        GameObject newExp = Instantiate(buttonPrefab);
        var ind = newExp.GetComponent<Index>();
        ind.index = index;
        var newExpTransform = newExp.GetComponent<RectTransform>();
        newExpTransform.SetParent(myTransform, false);
        newExpTransform.SetSiblingIndex(index);

        //TODO: See if theres a better way to acess button text
        newExpTransform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = exp.name;

    }

    private string TMP_DumbParseVector(float3 vec)
    {
        return "(" + vec.x.ToString() + ", " + vec.y.ToString() + ", " + vec.z.ToString() + ")";
    }
    private string TMP_DumbParseVector(int3 vec)
    {
        return "(" + vec.x.ToString() + ", " + vec.y.ToString() + ", " + vec.z.ToString() + ")";
    }

    public void ShowVariablesInputField()
    {
        var CrowdExperiment = BioCrowds.CrowdExperiment.instance;
        int currentSpawnArea = ExperimentManager.instance.curentSpawnAreaIndex;

        foreach (var item in parameterNameToInputField.Keys)
        {
            var textComp = parameterNameToInputField[item].GetComponent<TMP_InputField>();
           

            switch (item)
            {
                case "name":
                    textComp.text = ExperimentManager.instance.currentExp.name;
                    Debug.Log(textComp.text);
                    break;
                case "agentQuantity":
                    textComp.text = CrowdExperiment.SpawnAreas[currentSpawnArea].qtd.ToString();

                    break;
                case "Goal":
                    //Debug.Log(((Vector3)CrowdExperiment.SpawnAreas[currentSpawnArea].goal).ToString());

                    textComp.text = TMP_DumbParseVector(CrowdExperiment.SpawnAreas[currentSpawnArea].goal);


                    break;
                case "SpawnMin":
                    textComp.text = TMP_DumbParseVector(CrowdExperiment.SpawnAreas[currentSpawnArea].min);
                    break;
                case "SpawnMax":
                    textComp.text = TMP_DumbParseVector(CrowdExperiment.SpawnAreas[currentSpawnArea].max);

                    break;
                case "MaxSpeed":
                    textComp.text = CrowdExperiment.SpawnAreas[currentSpawnArea].maxSpeed.ToString();

                    break;
                //case "name":
                //    break;
                //case "name":
                //    break;
                //case "name":
                //    break;
                //case "name":
                //    break;

            }
        }
    }


    internal void PreviousSpawnArea(int curentSpawnAreaIndex)
    {
        throw new NotImplementedException();
    }

    internal void NextSpawnArea(int curentSpawnAreaIndex)
    {
        throw new NotImplementedException();
    }
}