using System;
using System.Collections.Generic;
using Unity.Mathematics;
//using UnityEditor.SceneManagement;
using UnityEngine;

namespace BioCrowds
{
    [Serializable]
    public class CrowdExperiment : ISettings
    {
        [System.Serializable]
        public class SpawnArea
        {
            public int qtd;
            public int3 max;
            public int3 min;
            public float3 goal;
            public float maxSpeed;
        
        }

        public List<SpawnArea> SpawnAreas;


        public class ObstacleArea
        {
            public float3 start;
            public float3 end;
        }

        public List<ObstacleArea> obstacleAreas;

        public static CrowdExperiment instance;

        public float agentRadius = 1f;
        public float markerRadius = 0.1f;
        public float MarkerDensity = 0.65f;
        public int TerrainX = 100;
        public int TerrainZ = 50;
        public float FramesPerSecond = 30f;



        public bool showMarkers = false;
        public bool showCells = false;

        #region GETTER_SETTER

        public void SetGoal(string s , int index)
        {
            //Remove blank spaces
            s = s.Replace(" ", "");
            var floats = s.Split(',');

            SpawnAreas[index].goal.x = float.Parse(floats[0]);
            SpawnAreas[index].goal.y = float.Parse(floats[1]);
            SpawnAreas[index].goal.z = float.Parse(floats[2]);
            



            
        }

        public float getAgentRadius() {
            return this.agentRadius;
        }

        public void setAgentRadius(float agentRadius) {
            this.agentRadius = agentRadius;
        }

        public float getMarkerRadius() {
            return this.markerRadius;
        }

        public void setMarkerRadius(float markerRadius) {
            this.markerRadius = markerRadius;
        }

        public float getMarkerDensity() {
            return this.MarkerDensity;
        }

        public void setMarkerDensity(float MarkerDensity) {
            this.MarkerDensity = MarkerDensity;
        }

        public int getTerrainX() {
            return this.TerrainX;
        }

        public void setTerrainX(int TerrainX) {
            this.TerrainX = TerrainX;
        }

        public int getTerrainZ() {
            return this.TerrainZ;
        }

        public void setTerrainZ(int TerrainZ) {
            this.TerrainZ = TerrainZ;
        }

        public float getFramesPerSecond() {
            return this.FramesPerSecond;
        }

        public void setFramesPerSecond(float FramesPerSecond) {
            this.FramesPerSecond = FramesPerSecond;
        }

        public bool isShowMarkers() {
            return this.showMarkers;
        }

        public void setShowMarkers(bool showMarkers) {
            this.showMarkers = showMarkers;
        }

        public bool isShowCells() {
            return this.showCells;
        }

        public void setShowCells(bool showCells) {
            this.showCells = showCells;
        }

        public List<SpawnArea> getSpawnAreas() {
            return this.SpawnAreas;
        }

        public void setSpawnAreas(List<SpawnArea> SpawnAreas) {
            this.SpawnAreas = SpawnAreas;
        }
 

        #endregion






        public void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);

            SpawnAreas = new List<SpawnArea>();
            obstacleAreas = new List<ObstacleArea>();

        }




        public override void SaveExperimentToFile()
        {
            throw new NotImplementedException();
        }

        public override void SetExperiment(ISettings exp)
        {
            instance = (CrowdExperiment)(exp);
        }

        public override void LoadExperimentFromFile()
        {
            string ExperimentName = "BaseExperimentFluid.json";

            var folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            var bioCrowdsFolder = System.IO.Directory.CreateDirectory(folder + "\\VHLAB\\BioCrowds");


            string settingsFile = bioCrowdsFolder.FullName + "\\" + ExperimentName;
            bool basisCase = System.IO.File.Exists(settingsFile);
            //Debug.Log(basisCase + " " + settingsFile);

            if (!basisCase)
                System.IO.File.WriteAllText(settingsFile, JsonUtility.ToJson(CrowdExperiment.instance, true));
            else
            {
                Debug.Log("Reading Experiment File");
                string file = System.IO.File.ReadAllText(settingsFile);
                JsonUtility.FromJsonOverwrite(file, instance);
            }
        }
    }
}
