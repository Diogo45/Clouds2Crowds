using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using BioCrowds;

[UpdateAfter(typeof(AgentMovementSystem)), UpdateAfter(typeof(EndFrameCounter))]
public class VisualizationSystem : ComponentSystem
{
    [System.Serializable]
    public struct AgentRecord
    {
        public int AgentID;
        public float3 Position;
        public int Ragdoll;
        public float3 particleCollisionPos;
        public float3 particleCollisionVel;


        public override string ToString()
        {
            string head = string.Format("{0:D0};{1:F3};{2:F3}",
                AgentID,
                Position.x,
                Position.z
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

    public List<AgentRecord> CurrentAgentPositions = new List<AgentRecord>();
    public int CurrentFrame { get { return complete.frame; } }

    public int frames = 0;

   

    public struct AgentGroup
    {
        public ComponentDataArray<Position> Position;
        [ReadOnly] public ComponentDataArray<AgentData> Data;
        [ReadOnly] public readonly int Length;
    }
    [Inject] public AgentGroup agentGroup;         

    protected override void OnUpdate()
    {
        //clear processing list
        processing.records.Clear();
        processing.frame = ++frames;

        for (int i = 0; i < agentGroup.Length; i++)
        {
            int Ragdoll = 0;
            float3 particleCollisionPos = float3.zero;
            float3 particleCollisionVel = float3.zero;

            if (FluidSettings.instance.Enabled)
            {
                World.Active.GetExistingManager<FluidMovementOnAgent>().AgentRecords.TryGetValue(agentGroup.Data[i].ID, out AgentRecord flag);
                Ragdoll = flag.Ragdoll;
                particleCollisionPos = flag.particleCollisionPos;
                particleCollisionVel = flag.particleCollisionVel;
            }

            processing.records.Add(new AgentRecord
            {
                AgentID = agentGroup.Data[i].ID,
                Position = agentGroup.Position[i].Value,
                Ragdoll = Ragdoll,
                particleCollisionPos = particleCollisionPos,
                particleCollisionVel = particleCollisionVel
                //Position = WindowManager.Crowds2Clouds(agentGroup.Position[i].Value),
                //CloudID = agentGroup.OwnerCloud[i].CloudID
            });

        }

        //update complete
        FrameRecord aux = complete;
        complete = processing;
        CurrentAgentPositions = complete.records;
        processing = aux;


        //var inst = BioClouds.Parameters.Instance;

        //if (!inst.SaveSimulationData)
        //    return;


        #region BioCrowds DataRecording update CurrentAgentPosition        

        //if (inst.MaxSimulationFrames == CurrentFrame - 1)
        //{
        //using (System.IO.StreamWriter file =
        //new System.IO.StreamWriter("Agents.txt", true))
        //{
        //    file.Write(complete.ToString() + '\n');
        //}
        ////}

        #endregion

    }


    protected override void OnCreateManager()
    {


        //base.OnCreateManager();

        //using (System.IO.StreamWriter file =
        //new System.IO.StreamWriter("Agents.txt", false))
        //{
        //    file.Write("#This file stores the Agent Data for each Agent." + '\n' +
        //    "#CurrentFrame;AgentsInFrame;AgentID1;CloudID;AgentPositionx1;AgentPositiony1;AgentID2;AgentPositionx2;AgentPositiony2;...;" + '\n');
        //}

        //using (System.IO.StreamWriter file =
        //new System.IO.StreamWriter("FrameTimes.txt", false))
        //{
        //    file.Write("#This file stores the processing time for each frame. Measured in Seconds." + '\n');
        //}

    }



}


