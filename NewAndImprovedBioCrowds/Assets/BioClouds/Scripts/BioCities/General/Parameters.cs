using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;


namespace BioClouds
{
    [System.Serializable]
    public struct MeshMaterial
    {
        public Mesh mesh;
        public Material mat;
    }

    /// <summary>
    /// Parameter configuration file.
    /// </summary>
    public class Parameters : MonoBehaviour
    {
        public void LoadOrGenerateParameters()
        {
            string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string customParametersTotalDirPath = string.Join("\\", userFolder, PartialParametersDirPath);


            System.IO.Directory.CreateDirectory(customParametersTotalDirPath);
            string filePath = string.Join("\\", customParametersTotalDirPath, "Settings.json");

            if (!System.IO.File.Exists(filePath))
                SaveCurrentState(filePath);

            string json = System.IO.File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(json, this);

            LogFilePath = string.Join("\\", userFolder, PartialParametersDirPath, PartialLogFilePath);
            ExperimentPath = string.Join("\\", userFolder, PartialParametersDirPath, PartialExperimentPath);

        }

        public void SaveCurrentState(string path)
        {

            string jsonThis = JsonUtility.ToJson(this, true);
            System.IO.File.WriteAllText(path, jsonThis);
            //Generates a default parameters file.
        }


        public static Color Density2Color(float density, int idowner)
        {
            float capped_density = math.min(Parameters.Instance.MaxColorDensity, density);

            float sample = capped_density / Parameters.Instance.MaxColorDensity;
            return new Color(sample, sample, sample, 1.0f);
        }

        public Texture2D GetHeatScaleTexture()
        {
            return GetHeatScaleTexture(HeatMapColors, HeatMapColors.Length);
        }
        public static Texture2D GetHeatScaleTexture(Color[] colors, int HeatmapSize)
        {
            Texture2D heatmapScale = new Texture2D(HeatmapSize, 1);
            Color[] heatmap_mat = new Color[HeatmapSize];

            for (int i = 0; i < (colors.Length - 1); i++)
            {
                int slice = HeatmapSize / (colors.Length - 1);

                for (int j = 0; j < slice; j++)
                {
                    if (i * slice + j >= HeatmapSize) break;
                    heatmap_mat[i * slice + j] = Color.Lerp(colors[i], colors[i + 1], j / (float)slice);
                }
            }
            heatmapScale.SetPixels(heatmap_mat);
            heatmapScale.Apply();
            heatmapScale.wrapMode = TextureWrapMode.Clamp;
            return heatmapScale;
        }

        public void Start()
        {
            InitializeParameters();     
        }

        public void InitializeParameters()
        {

            LoadOrGenerateParameters();

            FixedParameters.HeatMapTexture = Parameters.GetHeatScaleTexture(HeatMapColors, HeatMapScaleSize);

        }



        public string PartialParametersDirPath =  "VHLAB\\BioClouds\\";
        private static Parameters instance;
        public static Parameters Instance { get { if (instance == null) instance = GameObject.FindObjectOfType<Parameters>(); return instance; } }

        private FixedParameters _fixedParameters;
        public  FixedParameters FixedParameters { get { if (_fixedParameters == null) _fixedParameters = GameObject.FindObjectOfType<FixedParameters>(); return _fixedParameters; } } 


        public float CloudSpeed = 1.3f;                         //CloudMaxSpeed
        public float CloudGoalThreshold = 0.01f;                 //Cloud goal-checking distance
        public float CellWidth = 2f;                          //Cell width

        
        

        public float MaxColorDensity = 10f;
        public Color[] HeatMapColors;

        public int HeatMapScaleSize = 512;

        public int Rows { get { return (int)((DefaultDomainMaxX - DefaultDomainMinX) / CellWidth); } }
        public int Cols { get { return (int)((DefaultDomainMaxY - DefaultDomainMinY) / CellWidth); } }

        private float _cellArea = 0.0f;
        public float CellArea { get { if (_cellArea == 0.0f) _cellArea = CellWidth * CellWidth; return _cellArea; } }




        public string PartialExperimentPath;
        public string ExperimentPath;

        [HideInInspector]
        public float DefaultDomainMinX;
        [HideInInspector]
        public float DefaultDomainMaxX;
        [HideInInspector]
        public float DefaultDomainMinY;
        [HideInInspector]
        public float DefaultDomainMaxY;


        public bool DrawCloudToMarkerLines = true;
        public bool EnableRightPreference = true;

        public int SimulationFramesPerSecond = 10;

        public int MaxSimulationFrames = 2000;
        public int FramesForDataSave = 1;
        public float IDToRecord = -1;

        public bool SaveSimulationData = false;

        public string PartialLogFilePath = "Experiments\\Logs\\output";
        public string LogFilePath = "";


        public bool BioCloudsActive = true;
        public bool RenderClouds = true;
    }


}

