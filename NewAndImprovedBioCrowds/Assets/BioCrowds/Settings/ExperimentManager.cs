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
    public int curentSpawnAreaIndex;

    public List<Experiment> experiments;


    private int numExp = 1;
    private Dictionary<int, Experiment> experimentDict;
    public string Directory { get; private set; }


    public void OnApplicationQuit()
    {
        Exit();
    }

    public void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
        

        currentExp.settings = new List<ISettings>();

        currentExp.settings.Add(BioCrowds.CrowdExperiment.instance);
        currentExp.settings.Add(SimulationConstants.instance);
        currentExp.settings.Add(ControlVariables.instance);
        currentExp.settings.Add(FluidSettings.instance);
        //VICTOR
        currentExp.settings.Add(InteractionSettings.instance);


        currentExp.activeSettings = new List<System.Type>();

        currentExp.activeSettings.Add(typeof(BioCrowds.CrowdExperiment));
        currentExp.activeSettings.Add(typeof(SimulationConstants));
        currentExp.activeSettings.Add(typeof(ControlVariables));
        currentExp.activeSettings.Add(typeof(FluidSettings));


        //VICTOR
        currentExp.activeSettings.Add(typeof(InteractionSettings));

        currentExp.name = "EXPERIMENT 1";
        Directory = @"E:\PUCRS\Clouds2Crowds\NewAndImprovedBioCrowds\Experiments" + "\\" + currentExp.name;

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
        curentSpawnAreaIndex = 0;
        Directory = Application.dataPath + "\\" + currentExp.name;
    }


    public void Play()
    {

        Time.timeScale = SimulationConstants.instance.BioCrowdsTimeStep;


        var dirs = System.IO.Directory.GetDirectories(@"E:\PUCRS\Clouds2Crowds\NewAndImprovedBioCrowds\Experiments");

        int repeated = 0;

        foreach(string s in dirs)
        {
            if (s.Contains(currentExp.name)) repeated++;
            Debug.Log(s + " " + currentExp.name);
        }

        currentExp.name = currentExp.name + (repeated > 0 ? " (" + repeated + ")" : "");

        Directory = @"E:\PUCRS\Clouds2Crowds\NewAndImprovedBioCrowds\Experiments" + "\\" + currentExp.name;
        System.IO.Directory.CreateDirectory(Directory);


        

        BioCrowds.CrowdExperimentModuleManager.instance.Enable();
        if (IsExperimentActive(typeof(FluidSettings)) != -1 && currentExp.settings[IsExperimentActive(typeof(FluidSettings))].Enabled)
        {
            BioCrowds.FluidExperimentModuleManager.instance.Enable();
        }


        //VICTOR
        if (IsExperimentActive(typeof(FluidSettings)) != -1 && currentExp.settings[IsExperimentActive(typeof(FluidSettings))].Enabled)
        {
            InteractionModuleManager.instance.Enable();
        }

        ExperimentListManager.instance.Play();




    }


    

    public void Exit()
    {
        if (IsExperimentActive(typeof(FluidSettings)) != -1 && currentExp.settings[IsExperimentActive(typeof(FluidSettings))].Enabled)
        {
            BioCrowds.FluidExperimentModuleManager.instance.Disable();
            
        }
    }


    

    private int IsExperimentActive(System.Type type)
    {
        if (currentExp.activeSettings.Contains(type)) return currentExp.activeSettings.IndexOf(type);
        return -1;
    }



    public void SetGoal(string s)
    {
        if (IsExperimentActive(typeof(BioCrowds.CrowdExperiment)) != -1)
        {
            BioCrowds.CrowdExperiment.instance.SetGoal(s, curentSpawnAreaIndex);
        }

    }

    public void SetMin(string s)
    {
        if (IsExperimentActive(typeof(BioCrowds.CrowdExperiment)) != -1)
        {
            BioCrowds.CrowdExperiment.instance.SetMin(s, curentSpawnAreaIndex);
        }

    }


    public void SetMax(string s)
    {
        if (IsExperimentActive(typeof(BioCrowds.CrowdExperiment)) != -1)
        {
            BioCrowds.CrowdExperiment.instance.SetMax(s, curentSpawnAreaIndex);
        }

    }


    public void SetQtd(string s)
    {
        if (IsExperimentActive(typeof(BioCrowds.CrowdExperiment)) != -1)
        {
            BioCrowds.CrowdExperiment.instance.SetAgentQTD(s, curentSpawnAreaIndex);
        }

    }


    public void SetMaxSpeed(string s)
    {
        if (IsExperimentActive(typeof(BioCrowds.CrowdExperiment)) != -1)
        {
            BioCrowds.CrowdExperiment.instance.SetMaxSpeed(s, curentSpawnAreaIndex);
        }

    }

    public void NextSpawnArea()
    {
        if(curentSpawnAreaIndex < BioCrowds.CrowdExperiment.instance.SpawnAreas.Count - 1)
        {
            curentSpawnAreaIndex++;
            ExperimentListManager.instance.NextSpawnArea(curentSpawnAreaIndex);
        }
    }
    public void PreviousSpawnArea()
    {
        if (curentSpawnAreaIndex > 0)
        {
            curentSpawnAreaIndex--;
            ExperimentListManager.instance.PreviousSpawnArea(curentSpawnAreaIndex);
        }
    }

    public void AddSpawnArea()
    {
        BioCrowds.CrowdExperiment.instance.SpawnAreas.Add(new BioCrowds.CrowdExperiment.SpawnArea());
    }

    public void DeleteSpawnArea()
    {
        if (curentSpawnAreaIndex > 0)
        {
            BioCrowds.CrowdExperiment.instance.SpawnAreas.RemoveAt(curentSpawnAreaIndex);
            curentSpawnAreaIndex--;
        }
        
    }

    public void WriteName(string input)
    {
        currentExp.name = input;
        Debug.Log(currentExp.name);
    }


   




}
