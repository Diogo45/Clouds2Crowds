using System;

using Unity.Mathematics;
//using UnityEditor.SceneManagement;
using UnityEngine;

namespace BioCrowds
{
    [Serializable]
    public class CrowdExperiment : ISettings
    {
        [System.Serializable]
        public struct SpawnArea
        {
            public int qtd;
            public int3 max;
            public int3 min;
            public float3 goal;
            public float maxSpeed;
        }

        public struct ObstacleArea
        {
            public float3 start;
            public float3 end;
        }

        public static CrowdExperiment instance;

        public float agentRadius = 1f;
        public float markerRadius = 0.1f;
        public float MarkerDensity = 0.65f;
        public int TerrainX = 100;
        public int TerrainZ = 50;
        public float FramesPerSecond = 32f;


        public bool showMarkers = false;
        public bool showCells = false;

        #region GETTER_SETTER
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

        public SpawnArea[] getSpawnAreas() {
            return this.SpawnAreas;
        }

        public void setSpawnAreas(SpawnArea[] SpawnAreas) {
            this.SpawnAreas = SpawnAreas;
        }
 

        #endregion


        public SpawnArea[] SpawnAreas= { new SpawnArea{qtd = 50,
                                         goal = new float3{x = 100, y = 0, z = 25},
                                         max = new int3 {x = 15, y = 0, z = 40},
                                         min = new int3 {x = 0, y = 0, z = 10 },
                                         maxSpeed = 1.3f}
                                        };




        public void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);

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
