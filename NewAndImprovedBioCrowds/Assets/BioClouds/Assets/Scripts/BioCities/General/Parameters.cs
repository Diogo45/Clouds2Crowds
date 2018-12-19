using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

namespace BioCities
{
    [System.Serializable]
    public struct MeshMaterial
    {
        public Mesh mesh;
        public Material mat;
    }


    public class Parameters : MonoBehaviour
    {
        [System.Serializable]
        public enum DistanceFunction
        {
            Euclidian,
            Power,
            MinRadiusSubtraction
        }

        public static Color Density2Color(float density, int idowner)
        {
            float capped_density = math.min(Parameters.Instance.MaxColorDensity, density);

            float sample = capped_density / Parameters.Instance.MaxColorDensity;
            return new Color(sample, sample, sample, 1.0f);
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


        private static Parameters instance;

        public static Parameters Instance { get { if (instance == null) instance = GameObject.FindObjectOfType<Parameters>(); return instance; } }

        public int CloudCount;                         //Agents to randomly spawn

        public float MinAuxinRadius;                     //Minimum Minimum-Auxin-Auxin distance 
        public float MaxAuxinRadius;                     //Maximum Minmum-Auxin-Auxin distance
        public float AgentSpeed;                         //Agent Max Speed
        public float CloudSpeed;                         //CloudMaxSpeed
        public float AgentCloudAggregationRange;         //Agent to cloud aggregation range
        public float AgentGoalThreshold;                 //Agent goal-checking distance 
        public float CloudGoalThreshold;                 //Cloud goal-checking distance
        public float CellWidth;                          //Cell width

        public float CloudMaxRadius;
        public float CloudMinRadius;

        public float LocalMinimaPedalation;

        public DistanceFunction DistanceFunctionToUse;

        public float MaxColorDensity;
        public Color[] HeatMapColors;
        public Texture2D HeatMapTexture;
        public int HeatMapScaleSize;

        public int Rows { get { return (int)((DomainMaxX - DomainMinX) / CellWidth); } }
        public int Cols { get { return (int)((DomainMaxY - DomainMinY) / CellWidth); } }

        private float _cellArea = 0.0f;
        public float CellArea { get { if (_cellArea == 0.0f) _cellArea = CellWidth * CellWidth; return _cellArea; } }

        
        public MeshMaterial[] CellRendererData;

        public MeshMaterial[] CloudRendererData;

        public MeshMaterial[] HeatQuadRendererData;


        public string ExperimentPath;

        [HideInInspector]
        public float DomainMinX;
        [HideInInspector]
        public float DomainMaxX;
        [HideInInspector]
        public float DomainMinY;
        [HideInInspector]
        public float DomainMaxY;


        public bool DrawCloudToMarkerLines;
        public bool enableCloudSplitSystem;

        public int SimulationFramesPerSecond;

        public int MaxSimulationFrames;
        public int FramesForDataSave;

        public bool SaveDenstiies;
        public bool SavePositions;
        public int[] Rulers;
        public string LogFile;

        public GameObject heattextquad;

    }


}

