
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace BioCrowds
{
    public class ExperimentRecorder : ComponentSystem
    {
        [System.Serializable]
        public struct AgentRecord
        {
            public int AgentID;
            public float3 Position;

            public override string ToString()
            {
                string head = string.Format("{0:D0};{1:F3};{2:F3};",
                    AgentID,
                    Position.x,
                    Position.y
                );
                return head;
            }
        }

        [System.Serializable]
        public class FrameRecord
        {
            public int frame;
            public List<AgentRecord> records;

            public override string ToString()
            {
                string head = string.Format("{0:D0};{1:D0};",
                    frame,
                    records.Count
                );

                string tail = string.Join("", records);
                return head + tail;
            }
        }

        public FrameRecord complete = new FrameRecord() { records = new List<AgentRecord>() };
        public FrameRecord processing = new FrameRecord() { records = new List<AgentRecord>() };

        public IReadOnlyList<AgentRecord> CurrentAgentPositions { get { return complete.records.AsReadOnly(); } }
        public int CurrentFrame { get { return complete.frame; } }

        public int frames = 0;

        public struct AgentGroup
        {
            public ComponentDataArray<Position> Position;
            public ComponentDataArray<AgentData> Data;
            [ReadOnly] public readonly int Length;
        }

        public int FrameCounter = 0;

        [Inject] AgentGroup agentGroup;


        protected override void OnUpdate()
        {
            processing.records.Clear();
            processing.frame = frames++;

            for (int i = 0; i < agentGroup.Length; i++)
            {
                processing.records.Add(new AgentRecord
                {
                    AgentID = agentGroup.Data[i].ID,
                    Position = agentGroup.Position[i].Value
                   
                });

            }

            FrameRecord aux = complete;
            complete = processing;
            processing = aux;

            #region BioCrowds DataRecording

            //if (inst.MaxSimulationFrames == CurrentFrame - 1)
            //{
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(Settings.instance.LogPath + @"\Agents.txt", true))
            {
                file.Write(complete.ToString() + '\n');
            }
            //}

            #endregion

        }

    }




}
