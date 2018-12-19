using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using static Unity.Mathematics.math;
using Unity.Transforms;


namespace BioCities
{

    public struct Auxin : IComponentData
    {
        public int ID;
        public int OwningAgent;
    }

    public class AuxinTagSystem : JobComponentSystem
    {

    }

    [UpdateAfter(typeof(AuxinTagSystem))]
    public class AuxinNotifySystem : JobComponentSystem
    {

    }
}
