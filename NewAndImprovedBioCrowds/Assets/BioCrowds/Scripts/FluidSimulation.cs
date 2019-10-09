
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
using System.Runtime.InteropServices;

namespace BioCrowds
{
    [UpdateAfter(typeof(FluidParticleToCell))]
    [UpdateBefore(typeof(AgentMovementSystem))]
    public class FluidMovementOnAgent : JobComponentSystem
    {
        [Inject] FluidParticleToCell m_fluidParticleToCell;
        public NativeHashMap<int3, float3> CellMomenta;
        public NativeHashMap<int3, float3> ParticleSetMass;

        private static float thresholdDist = 0.01f;
        //1 g/cm3 = 1000 kg/m3
        //Calculate based on the original SplishSplash code, mass = volume * density
        //Where density = 1000kg/m^3 and volume = 0.8 * particleDiameter^3
        private static float particleMass = 0.0001f;//kg
        private static float agentMass = 65f;
        private static float timeStep = 1f / 25f;

        
           
            
            


        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public readonly int Length;
        }
        [Inject] AgentGroup agentGroup;


        public struct CellGroup
        {
            [ReadOnly] public ComponentDataArray<CellName> CellName;
            [ReadOnly] public SubtractiveComponent<AgentData> Agent;
            [ReadOnly] public SubtractiveComponent<MarkerData> Marker;

            [ReadOnly] public readonly int Length;
        }
        [Inject] public CellGroup cellGroup;

        struct CalculateFluidMomenta : IJobParallelFor
        {
            [WriteOnly] public NativeHashMap<int3, float3>.Concurrent CellMomenta; 
            [WriteOnly] public NativeHashMap<int3, float3>.Concurrent ParticleSetMass; 
            [ReadOnly] public ComponentDataArray<CellName> CellName;
            [ReadOnly] public int frameSize;
            [ReadOnly] public NativeMultiHashMap<int3, int> CellToParticles;
            [ReadOnly] public NativeList<float3> FluidPos;
            [ReadOnly] public NativeList<float3> FluidVel;

            public void Execute(int index)
            {
                int3 key = CellName[index].Value;

                NativeMultiHashMapIterator<int3> it;
                int particleID;
                float3 M_r = float3.zero;

                int numPart = 0;

                bool keepgoing = CellToParticles.TryGetFirstValue(key, out particleID, out it);

                if (!keepgoing) return;

                //HACK: Assuming that the set of simulations has a lower height than the agent's
                //HACK: Aproximating Dv
                //TODO: Read timeStep from simulation header
                //if (particleID + frameSize >= FluidPos.Length) return;
                float3 vel = FluidVel[particleID] / timeStep;
                //float3 vel = (FluidPos[particleID + frameSize] - FluidPos[particleID]) / timeStep;

                float3 P = vel * particleMass;
                M_r += P;
                numPart++;

                while(CellToParticles.TryGetNextValue(out particleID, ref it)){
                    vel = FluidVel[particleID] / (timeStep);

                    P = vel * particleMass;
                    M_r += P;
                    numPart++;
                }
                ParticleSetMass.TryAdd(key, numPart*particleMass);
                CellMomenta.TryAdd(key, M_r);
              
            }
        }

        struct ApplyFluidMomentaOnAgents : IJobParallelFor
        {

            [ReadOnly] public NativeHashMap<int3, float3> CellMomenta;
            [ReadOnly] public NativeHashMap<int3, float3> ParticleSetMass;

            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            public ComponentDataArray<AgentStep> AgentStep;

            public void Execute(int index)
            {
                int3 cell = new int3((int)math.floor(AgentPos[index].Value.x / 2.0f) * 2 + 1, 0,
                                   (int)math.floor(AgentPos[index].Value.z / 2.0f) * 2 + 1);



                bool keepgoing = CellMomenta.TryGetValue(cell, out float3 particleVel);
                if (!keepgoing) return;

                keepgoing = ParticleSetMass.TryGetValue(cell, out float3 particleSetMass);
                if (!keepgoing) return;

                float3 oldVel = AgentStep[index].delta;
                //Debug.Log(index + " " + oldVel + " " + particleVel + " " + particleSetMass);
                //TOTAL INELASTIC COLLISION
                oldVel = (oldVel * agentMass + particleVel * particleSetMass) / (agentMass + particleSetMass);


                AgentStep[index] = new AgentStep { delta = oldVel };



            }
        }

        protected override void OnStartRunning()
        {

            CellMomenta = new NativeHashMap<int3, float3>((Settings.experiment.TerrainX * Settings.experiment.TerrainZ) / 4, Allocator.Persistent);
            ParticleSetMass = new NativeHashMap<int3, float3>((Settings.experiment.TerrainX * Settings.experiment.TerrainZ) / 4, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            CellMomenta.Dispose();
            ParticleSetMass.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            CellMomenta.Clear();
            ParticleSetMass.Clear();

            CalculateFluidMomenta momentaJob = new CalculateFluidMomenta
            {
                CellMomenta = CellMomenta.ToConcurrent(),
                CellName = cellGroup.CellName,
                CellToParticles = m_fluidParticleToCell.CellToParticles,
                FluidPos = m_fluidParticleToCell.FluidPos,
                FluidVel = m_fluidParticleToCell.FluidVel,
                frameSize = m_fluidParticleToCell.frameSize,
                ParticleSetMass = ParticleSetMass.ToConcurrent()

            };

            var momentaJobHandle = momentaJob.Schedule(cellGroup.Length, Settings.BatchSize, inputDeps);

            momentaJobHandle.Complete();

            DrawMomenta();

            ApplyFluidMomentaOnAgents applyMomenta = new ApplyFluidMomentaOnAgents
            {
                AgentPos = agentGroup.AgentPos,
                AgentStep = agentGroup.AgentStep,
                CellMomenta = CellMomenta,
                ParticleSetMass = ParticleSetMass

            };

            var applyMomentaHandle = applyMomenta.Schedule(agentGroup.Length, Settings.BatchSize, momentaJobHandle);

            applyMomentaHandle.Complete();



            m_fluidParticleToCell.FluidVel.Clear();

            return applyMomentaHandle;
        }

        private void DrawMomenta()
        {
            for (int i = 0; i < cellGroup.Length; i++)
            {
                int3 cell = cellGroup.CellName[i].Value;
                float3 particleVel;
                CellMomenta.TryGetValue(cell, out particleVel);
                Debug.DrawLine(new float3(cell), new float3(cell) + particleVel,Color.red);

            }
        }


    }

    [UpdateAfter(typeof(AgentMovementVectors))]
    [UpdateBefore(typeof(AgentMovementSystem))]
    public class FluidParticleToCell : JobComponentSystem
    {

        public NativeList<float3> FluidPos;
        public NativeList<float3> FluidVel;
        public NativeMultiHashMap<int3, int> CellToParticles;

        public int frameSize = 100000;//particles
        public int bufferSize;// number of particles times the number of floats of data of each particle, for now 3 for 3 floats

        public int frame = 0;
        private int last = 0;
        private bool first = true;
        private static float thresholdHeigth = 1000f;
        private const string memMapName = "unityMemMap";
        private const string memMapNameVel = "unityMemMapVel";

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public readonly int Length;
        }
        [Inject] AgentGroup agentGroup;

        struct FillCellFluidParticles: IJobParallelFor
        {
            [WriteOnly] public NativeMultiHashMap<int3, int>.Concurrent CellToParticles;
            [ReadOnly] public NativeList<float3> FluidPos;
            public int frameSize;
            public int frame;


            public void Execute(int index)
            {

                float3 ppos = FluidPos[index];
                //float3 ppos = FluidPos[index + (frameSize - 1) * frame];
                if (ppos.y > thresholdHeigth) return;

                int3 cell = new int3((int)math.floor(FluidPos[index].x / 2.0f) * 2 + 1, 0,
                                     (int)math.floor(FluidPos[index].z / 2.0f) * 2 + 1);
                

                CellToParticles.Add(cell, index);


            }
        }


        protected override void OnStartRunning()
        {
            CellToParticles = new NativeMultiHashMap<int3, int>(frameSize * (Settings.experiment.TerrainX * Settings.experiment.TerrainZ)/4, Allocator.Persistent);
            FluidPos = new NativeList<float3>(frameSize, Allocator.Persistent);
            FluidVel = new NativeList<float3>(frameSize, Allocator.Persistent);

        }


        protected override void OnCreateManager()
        {

            //BinParser(simFile);
            bufferSize = frameSize * 3;

            //TODO: Get frameSize from FluidSimulator
            //frameSize * 3 as there are 3 floats for every particle
            int map = AcessDLL.OpenMemoryShare(memMapName, bufferSize);
            switch (map)
            {
                case 0:
                    Debug.Log("Memory Map " + memMapName + " Created");
                    break;
                case -1:
                    Debug.Log("Memory Map " + memMapName + " Array too large");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;
                    
                case -2:
                    Debug.Log("Memory Map " + memMapName + " could not create file mapping object");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;
                case -3:
                    Debug.Log("Memory Map " + memMapName + " could not create map view of the file");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;
                default:
                    Debug.Log("A Memory Map " + memMapName + " Already Exists");
                    break;
            }

            map = AcessDLL.OpenMemoryShare(memMapNameVel, bufferSize);
            switch (map)
            {
                case 0:
                    Debug.Log("Memory Map "+ memMapNameVel +" Created");
                    break;
                case -1:
                    Debug.Log("Memory Map " + memMapNameVel + " Array too large");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;

                case -2:
                    Debug.Log("Memory Map " + memMapNameVel + " could not create file mapping object");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;
                case -3:
                    Debug.Log("Memory Map " + memMapNameVel + " could not create map view of the file");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;
                default:
                    Debug.Log("A Memory Map " + memMapNameVel + " Already Exists");
                    break;
            }


            Debug.Log("Fluid Simulation Initialized");
        }



        
        private void BinParser(string file)
        {

            var folder = Application.dataPath;
            var simFile = folder + "/" + Settings.experiment.FluidSimPath;
            if (!System.IO.File.Exists(simFile))
            {
                Debug.Log("Fluid Simulation File Not Found: " + simFile);
                this.Enabled = false;
                World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                //TODO: disable every fluid sim system
                return;
            }
            BinaryReader binread = new BinaryReader(File.Open(file, FileMode.Open));
            frameSize = binread.ReadInt32();
            Debug.Log("N Particles: " + frameSize);
            Debug.Log("Lenght of File: " + (binread.BaseStream.Length - 3 * sizeof(float)));
            while (binread.BaseStream.Position != (binread.BaseStream.Length - 3 * sizeof(float)))
            {

                float x = binread.ReadSingle();
                float y = binread.ReadSingle();
                float z = binread.ReadSingle();
                //TODO: Parametrize the translation and scale
                FluidPos.Add(new float3((x*10) + 35, y*10 + 20, z*20 + 25));
            }     
        }

        private void FillFrameParticlePositions()
        {

            float3 translate = new float3(5f,0f,25f);
            float3 scale = new float3(10f, 10f, 10f);

            float[] floatStream = new float[bufferSize];
            AcessDLL.ReadMemoryShare(memMapName, floatStream);
            for (int i = 0; i < floatStream.Length - 2; i += 3)
            {
                float x = floatStream[i];
                float y = floatStream[i + 1];
                float z = -floatStream[i + 2];
                //TODO: Parametrize the translation and scale]
                float3 pos = new float3(x, y, z) * scale + translate;
                FluidPos.Add(pos);

               
            }

            float[] floatStreamVel = new float[bufferSize];
            AcessDLL.ReadMemoryShare(memMapNameVel, floatStreamVel);
            for (int i = 0; i < floatStreamVel.Length - 2; i += 3)
            {
                float x = floatStreamVel[i];
                float y = floatStreamVel[i + 1];
                float z = -floatStreamVel[i + 2];
                //TODO: Parametrize the translation and scale]
                float3 vel = new float3(x, y, z) * scale;
                FluidVel.Add(vel);



            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {



            ////TODO: Clear taking 120ms, solve performance issues
            //if(frame >= FluidPos.Length / frameSize)
            //{
            //    this.Enabled = false;
            //    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
            //    return inputDeps;
            //}


            FillFrameParticlePositions();

            CellToParticles.Clear();

            var FillCellMomentaJob = new FillCellFluidParticles
            {
                CellToParticles = CellToParticles.ToConcurrent(),
                FluidPos = FluidPos,
                frame = frame,
                frameSize = frameSize
            };

            var FillCellJobHandle = FillCellMomentaJob.Schedule(frameSize, Settings.BatchSize, inputDeps);

            FillCellJobHandle.Complete();

            DebugFluid();
            frame++;

            FluidPos.Clear();
            
            return FillCellJobHandle;
        }



        private void DebugFluid()
        {
            for (int i = 0; i < frameSize; i++)
            {
                Debug.DrawLine(FluidPos[i], FluidPos[i]+FluidVel[i]/100f );
            }

        }
        protected override void OnDestroyManager()
        {
            FluidPos.Dispose();
            FluidVel.Dispose();
            CellToParticles.Dispose();
        }

        private void DebugFluidParser()
        {
            int i = last;
            for (; i < frameSize + last && i < FluidPos.Length; i++)
            {
                Debug.DrawLine(FluidPos[i], FluidPos[i] + new float3(0f, 0.01f, 0f), Color.blue);
            }
            last = i;

            if (i >= FluidPos.Length - 1)
            {
                last = 0;
                i = last;
            }
        }
    }

    public static class AcessDLL
    {
        //private const string UNITYCOM = "..\\UnityCom\\Release\\UnityCom.dll";
        private const string UNITYCOM = "..\\UnityCom\\x64\\Release\\UnityCom";
        [DllImport(UNITYCOM, EntryPoint = "Add")]
        public static extern float Add(float a, float b);

        [DllImport(UNITYCOM, EntryPoint = "IsOpen")]
        public static extern bool IsOpen(char[] memMapName);

        [DllImport(UNITYCOM, EntryPoint = "OpenMemoryShare")]
        public static extern int OpenMemoryShare(string memMapName, long bufSize);

        [DllImport(UNITYCOM, EntryPoint = "WriteMemoryShare")]
        public static extern bool WriteMemoryShare(string memMapName, float[] val, long offset = 0, long length = -1);

        [DllImport(UNITYCOM, EntryPoint = "ReadMemoryShare")]
        public static extern bool ReadMemoryShare(string memMapName, float[] val, long offset = 0, long length = -1);

        [DllImport(UNITYCOM, EntryPoint = "GetSize")]
        public static extern long GetSize(string memMapName);

        [DllImport(UNITYCOM, EntryPoint = "CloseAllMemoryShare")]
        public static extern bool CloseAllMemoryShare();

        [DllImport(UNITYCOM, EntryPoint = "CloseMemoryShare")]
        public static extern bool CloseMemoryShare(string memMapName);
    }




}

