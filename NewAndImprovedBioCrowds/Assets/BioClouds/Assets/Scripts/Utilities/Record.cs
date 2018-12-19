using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace BioCities
{
    #region Data Recording
    public struct Record
    {
        public int Frame;
        public int ID;
        public int Agents;
        public int Cells;
        public float3 Position;
        List<int> CellID;


        public Record(int _Frame, int _ID, int _Agents, int _Cells, List<int> _CellID, float3 _Position)
        {
            Frame = _Frame;
            ID = _ID;
            Agents = _Agents;
            Cells = _Cells;
            Position = _Position;
            CellID = _CellID;
        }


        public override string ToString()
        {
            string head = string.Format("{0:D0};{1:D0};{2:D0};{3:F3};{4:F3};{5:D0};",
                Frame,
                ID,
                Agents,
                Position.x,
                Position.y,
                Cells
                );

            string tail = string.Join(";", CellID);

            return head + tail;
        }
    }
    #endregion
}
