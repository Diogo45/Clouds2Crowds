using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ExperimentManager : MonoBehaviour
{




    [System.Serializable]
    public struct Experiment
    {
        public string name;
        public List<ISettings> settings;
        public List<System.Type> activeSettings;
    }

    public static ExperimentManager instance;


    public Experiment currentExp;

    public List<Experiment> experiments;



    private int numExp = 1;
    private Dictionary<int, Experiment> experimentDict;
    public string Directory { get; private set; }

    public void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
        Directory = Application.dataPath;

        currentExp.settings = new List<ISettings>();

        currentExp.settings.Add(BioCrowds.CrowdExperiment.instance);
        currentExp.settings.Add(SimulationConstants.instance);
        currentExp.settings.Add(ControlVariables.instance);

        currentExp.activeSettings = new List<System.Type>();

        currentExp.activeSettings.Add(typeof(BioCrowds.CrowdExperiment));
        currentExp.activeSettings.Add(typeof(SimulationConstants));
        currentExp.activeSettings.Add(typeof(ControlVariables));


        experimentDict = new Dictionary<int, Experiment>();
        experimentDict.Add(0, currentExp);
    }

    public void AddExperiment()
    {
        List<ISettings> tempSettings = new List<ISettings>(currentExp.settings);
        Experiment newExp = new Experiment { name = string.Copy(currentExp.name), settings = tempSettings, activeSettings = new List<System.Type>()};
        foreach (var item in tempSettings)
        {
            newExp.activeSettings.Add(item.GetType());

        }
        experiments.Add(newExp);
        experimentDict.Add(numExp, newExp);

        ExperimentListManager.instance.AddExperiment(newExp, numExp);

        numExp++;
    }

    public void LoadExperimentFromFile(string expPath)
    {
        //for(int i = 0; i < SettingClasses.)
    }

    public void SaveExperimentToFile()
    {
        foreach (var item in currentExp.settings)
        {
            item.SaveExperimentToFile();
        }
    }

    public void SetExperiment(int index)
    {
        var tempExpList = experimentDict[index].settings;

        for (int i = 0; i < tempExpList.Count; i++)
        {
            int isExpActive = IsExperimentActive(tempExpList[i].GetType());
            if (isExpActive == -1)
            {
                currentExp.activeSettings.Add(tempExpList[i].GetType());
                currentExp.settings.Add(tempExpList[i]);               
            } else 
                currentExp.settings[isExpActive].SetExperiment(tempExpList[i]);
        }

        currentExp.name = experimentDict[index].name;
    }

    private int IsExperimentActive(System.Type type)
    {
        if (currentExp.activeSettings.Contains(type)) return currentExp.activeSettings.IndexOf(type);
        return -1;
    }


    public void WriteName(string input)
    {
        currentExp.name = input;
    }


   




}
