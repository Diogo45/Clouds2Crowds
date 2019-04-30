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
    }

    public class TimeMachineSettings : MonoBehaviour
    {
       
        public static TimeExperiment experiment;

        public void Start()
        {
            var folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            var bioCrowdsFolder = System.IO.Directory.CreateDirectory(folder + "\\VHLAB\\BioCrowds");


            string settingsFile = bioCrowdsFolder.FullName + "\\TimeExperiment.json";
            bool basisCase = System.IO.File.Exists(settingsFile);
            //Debug.Log(basisCase + " " + settingsFile);

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
