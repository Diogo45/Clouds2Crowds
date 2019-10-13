using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Experiment definition class.
/// </summary>
[System.Serializable]
public class Experiment{

    
    //[System.Serializable]
    //public struct WayPoint
    //{
    //    public int WayPointID;
    //    public float x;
    //    public float y;
    //    public int[] Neighbours;
    //}


    [System.Serializable]
    public struct AgentRegions
    {
        public int GoalWayPointID;
        public float[] Goal;
        public int Type;
        public Region Region;
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

        public override string ToString()
        {
            return minX + " " + maxX + " " + minY + " " + maxY;
        }
    }

    public int SeedState;
    public Region Domain;
    public int FramesToRecord;
    public int IDToRecord;
    public int CloudDivisionLimit;
    public int CloudSizeLimit;
    public int CloudSplitTimer;
    [SerializeField]
    public Region[] CellRegions;
    public AgentRegions[] AgentTypes;

    public PathManager.WayPoint[] WayPoints;

}
