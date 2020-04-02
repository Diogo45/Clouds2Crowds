using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class Index : MonoBehaviour
{
    public int index;

    public void LoadExperiment()
    {
        ExperimentManager.instance.SetExperiment(index);
        ExperimentListManager.instance.ShowVariablesInputField();
    }



}