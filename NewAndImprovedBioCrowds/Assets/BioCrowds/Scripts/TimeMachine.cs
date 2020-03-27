using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace BioCrowds
{

    [DisableAutoCreation]
    [UpdateBefore(typeof(AgentMovementSystem)),
     UpdateAfter(typeof(AgentMovementVectors)),
      UpdateInGroup(typeof(MovementVectorsSystemGroup))]
    public class AgentMovementTimeMachine : JobComponentSystem
    {
        public static int3 pos2cell(float x, float z)
        {
            return new int3((int)math.floor(x / 2.0f) * 2 + 1, 0, (int)(math.floor(z / 2.0f)) * 2 + 1);
        }

        public static int densityIndex(float d)
        {
            return math.clamp((int)math.abs(d / 0.25f), 0, 9);
        }

        public static float EC(int totalAgents, float agentArea, int obstacleNumber, float obstacleWeight, float worldArea, float obstacleArea)
        {
            return math.min(1.0f, (totalAgents * agentArea + obstacleNumber * obstacleWeight) / (worldArea - obstacleArea));
        }

        static Unity.Mathematics.Random random;
        List<string> fileNames = new List<string> { "0.25", "0.5", "0.75", "1", "1.25", "1.5", "1.75", "2", "2.25" };

        public float ECvalue;

        //@TODO FIX this
        #region No lookie into this gambiarra TODO fix
        NativeArray<float> weibul0_25;
        NativeArray<float> weibul0_5;
        NativeArray<float> weibul0_75;
        NativeArray<float> weibul1_0;
        NativeArray<float> weibul1_25;
        NativeArray<float> weibul1_5;
        NativeArray<float> weibul1_75;
        NativeArray<float> weibul2_0;
        NativeArray<float> weibul2_25;
        NativeArray<float> weibul2_5;
        #endregion

        public void LoadDensityValues()
        {
            Debug.Log("loading density weibul distribution");
            string partialPath = settings.DensityFiles;
            foreach(string name in fileNames)
            {
                List<float> new_list = new List<float>();

                using(StreamReader sr = new StreamReader( partialPath + "Dens" + name + ".txt"))
                {
                    string s = sr.ReadLine();
                    System.Globalization.CultureInfo ci = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.CurrentCulture.Clone();
                    ci.NumberFormat.NumberDecimalSeparator = ".";
                    float f = float.Parse(s, ci);
                    new_list.Add(f);
                }
            }
        }


        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<AgentGoal>     AgentGoal;
            [ReadOnly] public ComponentDataArray<Position>      Position;
            [WriteOnly] public ComponentDataArray<AgentStep>    AgentStep;
            [ReadOnly] public readonly int Length;
        }


        [Inject] AgentGroup agentGroup;
        [Inject] MarkerSystem m_cellSystem;
        TimeExperiment settings;

        int counter = 0;
      

        struct TimeJumpPDR : IJobParallelFor
        {
            [ReadOnly] public NativeHashMap<int3, float> cellDensities;
            [ReadOnly] public ComponentDataArray<AgentGoal> AgentGoals;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;

            [ReadOnly] public float EnvironmentalComplexity;
            [ReadOnly] public int TimeJump;

            [ReadOnly] public NativeArray<float> weibul0_25;
            [ReadOnly] public NativeArray<float> weibul0_5 ;
            [ReadOnly] public NativeArray<float> weibul0_75;
            [ReadOnly] public NativeArray<float> weibul1_0 ;
            [ReadOnly] public NativeArray<float> weibul1_25;
            [ReadOnly] public NativeArray<float> weibul1_5 ;
            [ReadOnly] public NativeArray<float> weibul1_75;
            [ReadOnly] public NativeArray<float> weibul2_0 ;
            [ReadOnly] public NativeArray<float> weibul2_25;
            [ReadOnly] public NativeArray<float> weibul2_5;

            //[ReadOnly] public 

            public ComponentDataArray<AgentStep> AgentStep;

            public void Execute(int index)
            {

                float3 moveStep = float3.zero;
                float3 goalDir = math.normalize(AgentGoals[index].SubGoal - AgentPos[index].Value);


                //Linear Estimative
                float y = AgentStep[index].delta.y;
                float3 pdr = AgentStep[index].delta * TimeJump;


                
                //Environment Perturbation
                pdr *= (1- EnvironmentalComplexity);



                //Density Perturbation
                int3 cell = pos2cell(AgentPos[index].Value.x, AgentPos[index].Value.z);
                cellDensities.TryGetValue(cell, out float localDensity);
                if (localDensity == 0.0f) return;

                int weibulIndex = densityIndex(localDensity);
                float r = 0;
                
                switch (weibulIndex)
                {
                    case 0:
                        r = weibul0_25[random.NextInt(4999)];
                        break;
                    case 1:
                        r = weibul0_5[random.NextInt(4999)];
                        break;
                    case 2:
                        r = weibul0_75[random.NextInt(4999)];
                        break;
                    case 3:
                        r = weibul1_0[random.NextInt(4999)];
                        break;
                    case 4:
                        r = weibul1_25[random.NextInt(4999)];
                        break;
                    case 5:
                        r = weibul1_5[random.NextInt(4999)];
                        break;
                    case 6:
                        r = weibul1_75[random.NextInt(4999)];
                        break;
                    case 7:
                        r = weibul2_0[random.NextInt(4999)];
                        break;
                    case 8:
                        r = weibul2_25[random.NextInt(4999)];
                        break;
                    case 9:
                        r = weibul2_5[random.NextInt(4999)];
                        break;

                }
                Debug.Log(r);

                float3 IP = r * goalDir * TimeJump;

                pdr -= IP;

                moveStep = pdr;
                moveStep.y = y;
                
                AgentStep[index] = new AgentStep() { delta = moveStep };

            }

        }

        protected override void OnCreateManager()
        {

            settings = TimeMachineSettings.experiment;
            
            if (!settings.Enabled)
            {
                this.Enabled = false;
                return;
            }
            LoadDensityValues();
            counter = 0;
            random.InitState(324341);

            weibul0_25 = new NativeArray<float>(5000, Allocator.Persistent);
            weibul0_5 =  new NativeArray<float>(5000, Allocator.Persistent);
            weibul0_75 = new NativeArray<float>(5000, Allocator.Persistent);
            weibul1_0 =  new NativeArray<float>(5000, Allocator.Persistent);
            weibul1_25 = new NativeArray<float>(5000, Allocator.Persistent);
            weibul1_5 =  new NativeArray<float>(5000, Allocator.Persistent);
            weibul1_75 = new NativeArray<float>(5000, Allocator.Persistent);
            weibul2_0 =  new NativeArray<float>(5000, Allocator.Persistent);
            weibul2_25 = new NativeArray<float>(5000, Allocator.Persistent);
            weibul2_5 =  new NativeArray<float>(5000, Allocator.Persistent);

        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            if (weibul0_25.IsCreated)
            {
                weibul0_25.Dispose();
                weibul0_5.Dispose();
                weibul0_75.Dispose();
                weibul1_0.Dispose();
                weibul1_25.Dispose();
                weibul1_5.Dispose();
                weibul1_75.Dispose();
                weibul2_0.Dispose();
                weibul2_25.Dispose();
                weibul2_5.Dispose();
            }

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            settings = TimeMachineSettings.experiment;

            counter++;


            if (counter == settings.StartFrame)
            {
                ECvalue = EC(agentGroup.Length, settings.agentArea, settings.obstacleNumber, settings.obstacleWeight, settings.worldArea, settings.obstacleArea);
                Debug.Log("jumped");
                var TimeJumpJob = new TimeJumpPDR()
                {
                    AgentGoals = agentGroup.AgentGoal,
                    AgentPos = agentGroup.Position,
                    AgentStep = agentGroup.AgentStep,
                    TimeJump = settings.FrameLeap,
                    
                    weibul0_25 = weibul0_25,
                    weibul0_5  = weibul0_5 ,
                    weibul0_75 = weibul0_75,
                    weibul1_0  = weibul1_0 ,
                    weibul1_25 = weibul1_25,
                    weibul1_5  = weibul1_5 ,
                    weibul1_75 = weibul1_75,
                    weibul2_0  = weibul2_0 ,
                    weibul2_25 = weibul2_25,
                    weibul2_5  = weibul2_5,
                    cellDensities = m_cellSystem.LocalDensities,
                    EnvironmentalComplexity = ECvalue
                };

                var TimeJumpJobDeps = TimeJumpJob.Schedule(agentGroup.Length, SimulationConstants.instance.BatchSize, inputDeps);

                TimeJumpJobDeps.Complete();
                return TimeJumpJobDeps;

            }
           
            return inputDeps;
        }
    }

}



