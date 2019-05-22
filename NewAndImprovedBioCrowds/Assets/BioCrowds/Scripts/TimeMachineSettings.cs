using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BioCrowds
{
    [System.Serializable]
    public class TimeExperiment
    {

        public bool Enabled = false;
        public int StartFrame = 100;
        public int FrameLeap = 100;
        public float EnvironmentalComplexity = 1.0f;
        public string DensityFiles = System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "\\VHLAB\\BioCrowds\\Weibuls\\").FullName;
        public float agentArea = 0.6f * 0.6f;
        public int obstacleNumber = 5;
        public float obstacleWeight = 0.5f;
        public float worldArea = 100f * 100f;
        public float obstacleArea = 500f;

    }

    public class TimeMachineSettings : MonoBehaviour
    {

        public static TimeExperiment experiment = new TimeExperiment();


        public void Awake()
        {
            var folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            var bioCrowdsFolder = System.IO.Directory.CreateDirectory(folder + "\\VHLAB\\BioCrowds");


            string settingsFile = bioCrowdsFolder.FullName + "\\TimeExperiment.json";
            bool basisCase = System.IO.File.Exists(settingsFile);
            Debug.Log(basisCase + " " + settingsFile);

            if (!basisCase)
                System.IO.File.WriteAllText(settingsFile, JsonUtility.ToJson(experiment, true));
            else
            {
                string file = System.IO.File.ReadAllText(settingsFile);
                experiment = JsonUtility.FromJson<TimeExperiment>(file);
            }
            
        }

        
}

}
