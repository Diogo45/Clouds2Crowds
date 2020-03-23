using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



public class ExperimentListManager : MonoBehaviour
{
    public static ExperimentListManager instance;

    public GameObject buttonPrefab;

    private RectTransform myTransform;


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


}