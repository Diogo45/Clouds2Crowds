using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;

namespace BioClouds
{

    public struct CellData : IComponentData
    {
        public int ID;
        public float Area;
    }

}
