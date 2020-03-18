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



    private int numExp = 0;
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

        experimentDict = new Dictionary<int, Experiment>();
    }

    public void AddExperiment()
    {
        List<ISettings> temp = new List<ISettings>(currentExp.settings);
        Experiment newExp = new Experiment { name = string.Copy(currentExp.name), settings = temp };
        foreach (var item in temp)
        {
            newExp.activeSettings.Add(item.GetType());

        }
        experiments.Add(newExp);
        experimentDict.Add(numExp++, newExp);

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
