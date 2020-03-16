using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class ISettings : MonoBehaviour
{
    public abstract void SaveExperimentToFile();

    public abstract void SetExperiment(ISettings exp);


}

