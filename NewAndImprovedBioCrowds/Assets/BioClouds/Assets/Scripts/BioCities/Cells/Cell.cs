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
    //grouping component
    public struct Cell : ISharedComponentData, System.IEquatable<Cell>
    {
        public int X;
        public int Y;

        public bool Equals(Cell other)
        {
            return X == other.X && Y == other.Y;
        }
    }
   

    public struct CellData : IComponentData
    {
        public int ID;
        public float Area;
    }

}
