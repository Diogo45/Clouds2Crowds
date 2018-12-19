using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;


namespace BioCities
{
    using static Unity.Mathematics.math;
    using Unity.Transforms;
    using Unity.Mathematics;

    public struct Agent : IComponentData
    {
        public int ID; //Agent unique numbering id
        public float3 EndGoal; //agent endgoal
        public float3 SubGoal; //agent current subgoal
    }

    public class AgentSystem : JobComponentSystem
    {
        //Map: agent -> subgoal list
        //Map: agent -> agent auxinlist

    }

    [UpdateAfter(typeof(CloudMoveSystem))]
    public class AgentMoveSystem : JobComponentSystem
    {
        //moves agents based on auxinlist
    }
    



}

