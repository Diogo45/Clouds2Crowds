using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Experiment{

    [System.Serializable]
    public struct AgentRegions
    {
        public float[] Goal;
        public int Type;
        public float[] Region;
        public int Quantity;
        public int CloudSize;
        public float PreferredDensity;
        public float RadiusChangeSpeed;
    }

    [System.Serializable]
    public struct Region
    {
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
    }

    public int SeedState;
    public string RadiusUpdateType;
    public float[] Domain;
    public int FramesToRecord;
    public int IDToRecord;
    [SerializeField]
    public Region[] CellRegions;
    public AgentRegions[] AgentTypes;
 
}
