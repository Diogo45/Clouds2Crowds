using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace BioClouds
{
    /// <summary>
    /// Cloud Movement Vector calculations.
    /// Follows the implementation of the BioClouds and BioCrowds paper.
    /// </summary>
    public static class CloudCalculations
    {
        //Current marker position, current cloud position and (goal position - cloud position) vector.
        public static float GetF(float3 markerPosition, float3 cloudPosition, float3 cloudGoalVector)
        {
            float Ymodule = math.length(markerPosition - cloudPosition);

            float Xmodule = math.length(cloudGoalVector);

            float dot = math.dot(markerPosition - cloudPosition, cloudGoalVector);

            if (Ymodule < 0.00001f)
                return 0.0f;
            
            return (1.0f / (1.0f + Ymodule)) * (1.0f + (dot / (Xmodule * Ymodule)));
        }

        public static float PartialW(float totalW, float fValue)
        {
            return fValue / totalW;
        }
    }
    
    public struct CloudData : IComponentData
    {
        public int ID;
        public int AgentQuantity; //TODO How many agents this blob symbolizes, quantity
        public float PreferredDensity; //Prefered agent density for the cloud
        public float Radius;
        public float RadiusChangeSpeed;
        public float MinRadius;
        public float MaxSpeed;
        public int Type;
        //public int ownerID; //current owner agent.
    }

    public struct CloudMoveStep : IComponentData {
        public float3 Delta;
    }

    public struct CloudGoal : IComponentData
    {
        public float3 SubGoal; //current cloud subgoal
        public float3 EndGoal; //current cloud endgoal
    }

    public struct CloudSplitData : IComponentData
    {
        public int splitCount;
        public int fatherID;
    }

    public struct CloudIDPosRadius
    {
        public float3 position;
        public int ID;
        public float Radius;
        public float MinRadius;
    }
    
    public struct CloudLateSpawn
    {
        public float3 position;
        public int agentQuantity;
        public float3 goal;
        public int cloudType;
        public float preferredDensity;
        public float radiusChangeSpeed;
        public int splitCount;
        public int fatherID;
        public float radiusMultiplier;
    }
    
}
