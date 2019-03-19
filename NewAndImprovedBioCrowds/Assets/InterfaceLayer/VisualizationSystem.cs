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

public struct BioCrowdsAnchor : IComponentData
{
    public float3 Pivot;
}
[UpdateAfter(typeof(BioCrowds.AgentMovementSystem))]
public class BioCrowdsPivotCorrectonatorSystemDeluxe : JobComponentSystem
{
    private float3 currentPivot;
    private bool dirtyFlag;

    public void PivotChange(float3 newPivot)
    {
        currentPivot = newPivot;
        dirtyFlag = true;
    }
    public struct AnchorCorrectGroup
    {
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<BioCrowdsAnchor> Pivot;
        [ReadOnly] public readonly int Length;
    };

    [Inject]public AnchorCorrectGroup m_AnchorGroup;
    
    public struct PositionCorrectonatorJob : IJobParallelFor
    {
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<BioCrowdsAnchor> Pivot;
        [ReadOnly] public float3 newPivot;

        public void Execute(int index)
        {
            float3 oldPos = Position[index].Value;
            float3 oldPivot = Pivot[index].Pivot;

            float3 newPos = WindowManager.ChangePivot(oldPos, oldPivot, newPivot);

            //Debug.Log("Pivots : " + newPivot + oldPivot + " Positions: " + oldPos + newPos);

            Position[index] = new Position { Value = newPos };
            Pivot[index] = new BioCrowdsAnchor { Pivot = newPivot };
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!dirtyFlag)
            return inputDeps;

        var job = new PositionCorrectonatorJob
        {
            Position = m_AnchorGroup.Position,
            Pivot = m_AnchorGroup.Pivot,
            newPivot = currentPivot
        };

        var jobHandle = job.Schedule(m_AnchorGroup.Length, 1, inputDeps);
        dirtyFlag = false;
        return jobHandle;
    }
}

[UpdateAfter(typeof(AgentMovementSystem)), UpdateAfter(typeof(EndFrameCounter))]
public class VisualizationSystem : ComponentSystem
{
    [System.Serializable]
    public struct AgentRecord
    {
        public int AgentID;
        public float3 Position;
        public int CloudID;

        public override string ToString()
        {
            string head = string.Format("{0:D0};{1:D0};{2:F3};{3:F3};",
                AgentID,
                CloudID,
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


    public List<BioClouds.Record> bioCloudsRecords = new List<BioClouds.Record>();

    public struct AgentGroup
    {
        public ComponentDataArray<Position> Position;
        [ReadOnly] public ComponentDataArray<AgentData> Data;
        [ReadOnly] public SharedComponentDataArray<AgentCloudID> OwnerCloud;
        [ReadOnly] public readonly int Length;
    }
    [Inject] public AgentGroup agentGroup;

    [Inject] public BioClouds.CellMarkSystem m_CellMarkSystem;


    public struct CloudDataGroup
    {
        [ReadOnly] public ComponentDataArray<BioClouds.CloudData> CloudData;
        [ReadOnly] public ComponentDataArray<Position> Position;
        [ReadOnly] public readonly int Length;
    }
    [Inject] public CloudDataGroup m_CloudDataGroup;


    protected override void OnUpdate()
    {
        processing.records.Clear();
        processing.frame = frames++;

        for (int i = 0; i < agentGroup.Length; i++)
        {
            processing.records.Add(new AgentRecord
            {
                AgentID = agentGroup.Data[i].ID,
                Position = WindowManager.Crowds2Clouds(agentGroup.Position[i].Value),
                CloudID = agentGroup.OwnerCloud[i].CloudID
            });
            
        }

        FrameRecord aux = complete;
        complete = processing;
        processing = aux;


        var inst = BioClouds.Parameters.Instance;

        if (!inst.SaveSimulationData)
            return;

        //Data recording
        #region BioClouds Datarecording
        NativeMultiHashMap<int, float3> cellmap = m_CellMarkSystem.cloudID2MarkedCellsMap;
        float3 currentCellPosition;
        NativeMultiHashMapIterator<int> it;
        
        //if ((inst.SaveDenstiies || inst.SavePositions))
        //{
            if (inst.MaxSimulationFrames > CurrentFrame && CurrentFrame % inst.FramesForDataSave == 0)
            {
                for (int i = 0; i < m_CloudDataGroup.Length; i++)
                {
                    List<int> cellIDs = new List<int>();

                    if (!cellmap.TryGetFirstValue(m_CloudDataGroup.CloudData[i].ID, out currentCellPosition, out it))
                        continue;
                    int2 grid_cell = GridConverter.PositionToGridCell(new float3(currentCellPosition.x, currentCellPosition.y, currentCellPosition.z));
                    cellIDs.Add(GridConverter.GridCell2CellID(grid_cell));

                    while (cellmap.TryGetNextValue(out currentCellPosition, ref it))
                    {
                        grid_cell = GridConverter.PositionToGridCell(new float3(currentCellPosition.x, currentCellPosition.y, currentCellPosition.z));
                        cellIDs.Add(GridConverter.GridCell2CellID(grid_cell));
                    }

                    if(inst.IDToRecord == -1 || m_CloudDataGroup.CloudData[i].ID == inst.IDToRecord)
                    {
                        BioClouds.Record record = new BioClouds.Record(frames,
                                                                       m_CloudDataGroup.CloudData[i].ID,
                                                                       m_CloudDataGroup.CloudData[i].AgentQuantity,
                                                                       cellIDs.Count,
                                                                       cellIDs,
                                                                       m_CloudDataGroup.Position[i].Value,
                                                                       m_CloudDataGroup.CloudData[i].Radius
                                                );

                        bioCloudsRecords.Add(record);
                    }
                    
                }
            }

            //if (inst.MaxSimulationFrames == CurrentFrame - 1)
            //{
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(inst.LogFilePath + "Clouds.txt", true))
            {
                foreach (BioClouds.Record record in bioCloudsRecords)
                    file.Write(record.ToString() + '\n');
            }
            bioCloudsRecords.Clear();
            //}
        //}
        #endregion


        #region BioCrowds DataRecording

        //if (inst.MaxSimulationFrames == CurrentFrame - 1)
        //{
        using (System.IO.StreamWriter file =
        new System.IO.StreamWriter(inst.LogFilePath + "Agents.txt", true))
        {
            file.Write(complete.ToString() + '\n');
        }
        //}

        #endregion

    }


    protected override void OnCreateManager()
    {


        base.OnCreateManager();
        var inst = BioClouds.Parameters.Instance;

        using (System.IO.StreamWriter file =
        new System.IO.StreamWriter(inst.LogFilePath + "Agents.txt", false))
        {
            file.Write("#This file stores the Agent Data for each Agent." + '\n' +
            "#CurrentFrame;AgentsInFrame;AgentID1;CloudID;AgentPositionx1;AgentPositiony1;AgentID2;AgentPositionx2;AgentPositiony2;...;" + '\n');
        }

        using (System.IO.StreamWriter file =
        new System.IO.StreamWriter(inst.LogFilePath + "Clouds.txt", false))
        {
            file.Write("#This file stores the Cloud Data for each cloud." + '\n' + 
                       "#CurrentFrame;CloudID;RadiusSize;AgentsInCloud;CloudPositionX;CloudPositionY;CapturedCellsQuantity;CellIDs;" + '\n');
        }

        using (System.IO.StreamWriter file =
        new System.IO.StreamWriter(inst.LogFilePath + "FrameTimes.txt", false))
        {
            file.Write("#This file stores the processing time for each frame. Measured in Seconds." + '\n');
        }

    }



}


