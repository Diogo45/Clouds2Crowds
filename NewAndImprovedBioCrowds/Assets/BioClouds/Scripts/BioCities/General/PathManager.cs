using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public static class PathManager{

    [System.Serializable]
    public struct WayPoint
    {
        public int ID;
        public float x;
        public float y;
        public int[] Neighbours;

        public override string ToString()
        {
            string neighs = "";
            foreach (int n in Neighbours)
                neighs += " " + n;

            return ID + " " + x + "," + y + " Neighbors :"+ neighs;
        }
    }


    private static Dictionary<int, WayPoint> WayPointList = new Dictionary<int, WayPoint>();


    public static void LoadWayPoints(WayPoint[] wayPoints)
    {

        foreach (WayPoint w in wayPoints)
        {

            Debug.Log(w);

            AddWayPoint(w);
        }
    }

   public static void AddWayPoint(WayPoint waypoint)
    {
        WayPointList.Add(waypoint.ID, waypoint);
    }

    public static WayPoint GetWayPointByID(int id)
    {
        WayPointList.TryGetValue(id, out WayPoint wayPoint);

        return wayPoint;
    }



    public static WayPoint GetNext(float3 position, int objectiveID)
    {
        WayPoint closest_wp = GetWayPointByID(objectiveID);
        float closestDistance = 1000000f;

        foreach(WayPoint w in WayPointList.Values)
        {
            float d = math.distance(position, new float3(w.x, w.y, 0));
            if (d  < closestDistance)
            {
                closestDistance = d;
                closest_wp = w;
            }
        }

        WayPoint next = GetNext(closest_wp.ID, objectiveID);

        
        return next;

    }

    public static WayPoint GetNext(int currentID, int objectiveID)
    {
        return GetNext(GetWayPointByID(currentID), GetWayPointByID(objectiveID));
    }

    public static WayPoint GetNext(WayPoint current, WayPoint objective)
    {
        HashSet<int> checkedIDs = new HashSet<int>();

        Queue<int> ids2Check = new Queue<int>();

        int currentNext = -1;

        checkedIDs.Add(current.ID);

        foreach(int n in current.Neighbours)
        {
            currentNext = n;
            ids2Check.Enqueue(n);

            while (ids2Check.Count > 0)
            {
                int neigh = ids2Check.Dequeue();

                if (checkedIDs.Contains(neigh))
                    continue;

                if (GetWayPointByID(neigh).ID == objective.ID)
                {
                    return GetWayPointByID(currentNext);
                }


                checkedIDs.Add(neigh);

                foreach (int neighneigh in GetWayPointByID(neigh).Neighbours)
                    ids2Check.Enqueue(neighneigh);

            }

            checkedIDs.Add(n);
        }

        return objective;


    }


}
