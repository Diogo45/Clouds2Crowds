
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
using System.Collections;
using System.Threading;
using static FluidSettings;
using Unity.Rendering;

namespace BioCrowds
{
    public struct FluidData : IComponentData
    {
        public float tau;
    }

    public struct PhysicalData : IComponentData
    {
        public float mass;
    }


    [UpdateAfter(typeof(FluidParticleToCell)), UpdateAfter(typeof(AgentMassMapSystem))]
    [UpdateBefore(typeof(AgentMovementSystem))]
    public class FluidMovementOnAgent : JobComponentSystem
    {

        #region VARIABLES
        [Inject] public FluidParticleToCell m_fluidParticleToCell;
        public NativeHashMap<int3, float3> CellMomenta;
        public NativeHashMap<int3, float3> ParticleSetMass;

        public static int frame;

        public struct AgentParticlesData
        {
            public int frame;
            public float3 meanParticleVelocity;
            public float3 agentVel;
            public float stdDev;
            public float agentMass;
            public int numParticles;
        }

        public NativeHashMap<int, AgentParticlesData> AgentFluidData;









        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<PhysicalData> PhysicalData;
            public ComponentDataArray<FluidData> FluidData;
            public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public readonly int Length;
        }
        [Inject] public AgentGroup agentGroup;


        public struct CellGroup
        {
            [ReadOnly] public ComponentDataArray<CellName> CellName;
            [ReadOnly] public SubtractiveComponent<AgentData> Agent;
            [ReadOnly] public SubtractiveComponent<MarkerData> Marker;

            [ReadOnly] public readonly int Length;
        }
        [Inject] public CellGroup cellGroup;
        private bool wroteInitialParam = false;
        #endregion

      

        struct ApplyFluidMomentaOnAgents : IJobParallelFor
        {

            //[ReadOnly] public NativeHashMap<int3, float3> CellMomenta;
            //[ReadOnly] public NativeHashMap<int3, float3> ParticleSetMass;
            [ReadOnly] public ComponentDataArray<FluidData> FluidData;
            [ReadOnly] public ComponentDataArray<PhysicalData> PhysicalData;
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;

            [ReadOnly] public NativeMultiHashMap<int3, int> CellToParticles;
            [ReadOnly] public NativeList<float3> FluidPos;
            [ReadOnly] public NativeList<float3> FluidVel;


            [WriteOnly, NativeDisableParallelForRestriction] public NativeHashMap<int, AgentParticlesData>.Concurrent AgentFluidData;



            [ReadOnly] public float RestitutionCoef;

            public void Execute(int index)
            {
                int3 cell = new int3((int)math.floor(AgentPos[index].Value.x / 2.0f) * 2 + 1, 0,
                                   (int)math.floor(AgentPos[index].Value.z / 2.0f) * 2 + 1);

                float3 M_r = float3.zero;
                int numPart = 0;

                //Mean Vel for data gathering
                float3 meanVel = float3.zero;

                int startX = cell.x - 2;
                int startZ = cell.z - 2;
                int endX = cell.x + 2;
                int endZ = cell.z + 2;


                for (int i = startX; i <= endX; i = i + 2)
                {
                    for (int j = startZ; j <= endZ; j = j + 2)
                    {
                        NativeMultiHashMapIterator<int3> it;
                        int particleID;

                        cell = new int3(i, 0, j);


                        bool keepgoing = CellToParticles.TryGetFirstValue(cell, out particleID, out it);

                        if (!keepgoing)
                        {
                            continue;
                        }

                        float3 pos = FluidPos[particleID];
                        pos.y = 0f;
                        var agentPos = AgentPos[index].Value;
                        agentPos.y = 0;
                        float centerDist = math.distance(pos, agentPos);

                        if (centerDist < math.abs(FluidSettings.instance.personRadius - FluidSettings.instance.particleRadius))
                        {

                            float3 vel = FluidVel[particleID] * SimulationConstants.instance.BioCrowdsTimeStep;
                            meanVel += vel;
                            float3 P = vel * FluidSettings.instance.particleMass;
                            M_r += P;
                            numPart++;
                        }


                        //if (math.distance(pos, AgentPos[index].Value) < personRadius)
                        //{
                        //    M_r += P;
                        //    numPart++;
                        //}


                        while (CellToParticles.TryGetNextValue(out particleID, ref it))
                        {

                            pos = FluidPos[particleID];
                            pos.y = 0f;
                            agentPos = AgentPos[index].Value;
                            agentPos.y = 0;
                            centerDist = math.distance(pos, agentPos);


                            if (centerDist < math.abs(FluidSettings.instance.personRadius - FluidSettings.instance.particleRadius))
                            {
                                var vel = FluidVel[particleID] * SimulationConstants.instance.BioCrowdsTimeStep;

                                meanVel += vel;


                                var P = vel * FluidSettings.instance.particleMass;
                                M_r += P;
                                numPart++;
                            }
                        }


                    }
                }


                float3 particleSetMomenta = M_r;
                float particleSetMass = numPart * FluidSettings.instance.particleMass;



                //bool keepgoing = CellMomenta.TryGetValue(cell, out float3 particleVel);
                //if (!keepgoing) return;

                //keepgoing = ParticleSetMass.TryGetValue(cell, out float3 particleSetMass);
                //if (!keepgoing) return;

                float3 OldAgentVel = AgentStep[index].delta;
                float agentMass = PhysicalData[index].mass;
                //tau is how much the fluid acts on the agent, or (1 - tau) is how much the agent resists the fluid
                float tau = FluidData[index].tau;
                //TOTAL INELASTIC COLLISION
                if (numPart > 0)
                {
                    OldAgentVel = (OldAgentVel * agentMass + tau * particleSetMomenta) / (agentMass + tau * particleSetMass);
                }


                //HACK: For now, while we dont have ragdolls or the buoyancy(upthrust) force, not making use of the y coordinate 
                OldAgentVel.y = 0f;

                AgentStep[index] = new AgentStep { delta = OldAgentVel };

                //Data Gathering
                if (!meanVel.Equals(float3.zero) && numPart != 0)
                {
                    meanVel = meanVel / (float)numPart;
                }

                bool tryadd = AgentFluidData.TryAdd(AgentData[index].ID, new AgentParticlesData { frame = frame, agentMass = agentMass, meanParticleVelocity = meanVel, numParticles = numPart, agentVel = OldAgentVel });

            }
        }

        #region ON...
        protected override void OnStartRunning()
        {

            CellMomenta = new NativeHashMap<int3, float3>((CrowdExperiment.instance.TerrainX * CrowdExperiment.instance.TerrainZ) / 4, Allocator.Persistent);
            ParticleSetMass = new NativeHashMap<int3, float3>((CrowdExperiment.instance.TerrainX * CrowdExperiment.instance.TerrainZ) / 4, Allocator.Persistent);

            AgentFluidData = new NativeHashMap<int, AgentParticlesData>(ControlVariables.instance.agentQuantity * 2, Allocator.Persistent);

            System.IO.File.Delete(m_fluidParticleToCell.dataPath + "/fluidAgentData.txt");

            string text = "ID; Velocity in m/s; Qtd of Particles; Agent Mass in kg \n";
            System.IO.File.AppendAllText(m_fluidParticleToCell.dataPath + "/fluidAgentData.txt", text);

            //1 g/cm3 = 1000 kg/m3
            //Calculate based on the original SplishSplash code, mass = volume * density
            //Where density = 1000kg/m^3 and volume = 0.8 * particleDiameter^3
            float particleDiameter = 2 * FluidSettings.instance.particleRadius /** FluidSettings.instance.scale.x*/;
            float volume = 0.8f * math.pow(particleDiameter, 3);
            float density = 1000f;
            FluidSettings.instance.particleMass = volume * density;
            //particleMass = FluidSettings.instance.particleMass /** 10f*/;
            Debug.Log("Particle Mass: " + FluidSettings.instance.particleMass);

        }

        protected override void OnDestroyManager()
        {
            //CellMomenta.Dispose();
            //ParticleSetMass.Dispose();
            AgentFluidData.Dispose();
        }
        #endregion

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            //WriteData();
            AgentFluidData.Clear();

            frame = m_fluidParticleToCell.frame;

            //CellMomenta.Clear();
            //ParticleSetMass.Clear();

            //CalculateFluidMomenta momentaJob = new CalculateFluidMomenta
            //{
            //    CellMomenta = CellMomenta.ToConcurrent(),
            //    CellName = cellGroup.CellName,
            //    CellToParticles = m_fluidParticleToCell.CellToParticles,
            //    FluidPos = m_fluidParticleToCell.FluidPos,
            //    FluidVel = m_fluidParticleToCell.FluidVel,
            //    frameSize = m_fluidParticleToCell.frameSize,
            //    ParticleSetMass = ParticleSetMass.ToConcurrent()

            //};

            //var momentaJobHandle = momentaJob.Schedule(cellGroup.Length, SimulationConstants.instance.BatchSize, inputDeps);

            //momentaJobHandle.Complete();


            //DrawMomenta();

            ApplyFluidMomentaOnAgents applyMomenta = new ApplyFluidMomentaOnAgents
            {
                AgentPos = agentGroup.AgentPos,
                AgentStep = agentGroup.AgentStep,
                CellToParticles = m_fluidParticleToCell.CellToParticles,
                FluidPos = m_fluidParticleToCell.FluidPos,
                FluidVel = m_fluidParticleToCell.FluidVel,
                AgentData = agentGroup.AgentData,
                //CellMomenta = CellMomenta,
                AgentFluidData = AgentFluidData.ToConcurrent(),
                //ParticleSetMass = ParticleSetMass,
                FluidData = agentGroup.FluidData,
                PhysicalData = agentGroup.PhysicalData

            };

            //var applyMomentaHandle = applyMomenta.Schedule(agentGroup.Length, SimulationConstants.instance.BatchSize, momentaJobHandle);
            var applyMomentaHandle = applyMomenta.Schedule(agentGroup.Length, SimulationConstants.instance.BatchSize, inputDeps);
            applyMomentaHandle.Complete();

            //Write agent fluid collision data
            string text = "";

            for (int i = 0; i < agentGroup.Length; i++)
            {
                AgentFluidData.TryGetValue(agentGroup.AgentData[i].ID, out AgentParticlesData item);

                //text += agentGroup.AgentData[i].ID + ";" + Math.Sqrt(item.meanParticleVelocity.x * item.meanParticleVelocity.x + item.meanParticleVelocity.y * item.meanParticleVelocity.y + item.meanParticleVelocity.z * item.meanParticleVelocity.z) + ";" + item.numParticles + ";" + item.agentMass + "\n";
                text += item.frame + ";" + agentGroup.AgentData[i].ID + ";" + item.meanParticleVelocity + ";" + item.agentVel + ";" + item.numParticles + ";" + item.agentMass + "\n";


            }

            System.IO.File.AppendAllText(m_fluidParticleToCell.dataPath + "/fluidAgentData.txt", text);



            //TODO: Find a place out of update for initial parameters
            if (!wroteInitialParam)
            {
                InitialParameters par = new InitialParameters { mass = new float[ControlVariables.instance.agentQuantity], tau = new float[ControlVariables.instance.agentQuantity] };


                for (int i = 0; i < agentGroup.Length; i++)
                {
                    var tau = agentGroup.FluidData[i];
                    var mass = agentGroup.PhysicalData[i];

                    par.tau[i] = tau.tau;
                    par.mass[i] = mass.mass;
                }

                wroteInitialParam = true;
                FluidLogger.WriteInitalParam(par);
            }

            return applyMomentaHandle;
        }

        private void WriteData()
        {
            string data = "";
            for (int i = 0; i < cellGroup.Length; i++)
            {
                int3 cell = cellGroup.CellName[i].Value;
                float3 particleVel;
                CellMomenta.TryGetValue(cell, out particleVel);
                data += i + ";" + ((Vector3)particleVel).magnitude + "\n";

            }

            System.IO.File.AppendAllText(AcessDLL.dataPath, data);
        }

        private void DrawMomenta()
        {
            for (int i = 0; i < cellGroup.Length; i++)
            {
                int3 cell = cellGroup.CellName[i].Value;
                float3 particleVel;
                CellMomenta.TryGetValue(cell, out particleVel);
                Debug.DrawLine(new float3(cell), new float3(cell) + particleVel, Color.red);

            }
        }


    }

    [UpdateAfter(typeof(AgentMovementVectors))]
    [UpdateBefore(typeof(AgentMovementSystem))]
    public class FluidParticleToCell : JobComponentSystem
    {
        #region VARIABLES
        public NativeList<float3> FluidPos;
        public NativeList<float3> FluidVel;
        public NativeMultiHashMap<int3, int> CellToParticles;




        public int bufferSize;// number of particles times the number of floats of data of each particle, for now 3 for 3 floats


        public int frame = 0;
        public float timeSplish = 0f;
        private const string memMapName = "unityMemMap";
        private const string memMapNameVel = "unityMemMapVel";
        private const string memMapControl = "unityMemMapControl";

        private int Clones = 1;



        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
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
        #endregion

        struct FillCellFluidParticles : IJobParallelFor
        {
            [WriteOnly] public NativeMultiHashMap<int3, int>.Concurrent CellToParticles;
            [ReadOnly] public NativeList<float3> FluidPos;



            public void Execute(int index)
            {

                float3 ppos = FluidPos[index];
                //float3 ppos = FluidPos[index + (frameSize - 1) * frame];
                if (ppos.y > FluidSettings.instance.thresholdHeigth ||
                    ppos.x > CrowdExperiment.instance.TerrainX || ppos.z > CrowdExperiment.instance.TerrainZ ||
                    ppos.x < 0f || ppos.z < 0f) return;

                int3 cell = new int3((int)math.floor(FluidPos[index].x / 2.0f) * 2 + 1, 0,
                                     (int)math.floor(FluidPos[index].z / 2.0f) * 2 + 1);


                CellToParticles.Add(cell, index);


            }
        }



        #region ON...
        protected override void OnStartRunning()
        {



            OpenMemoryMap(memMapControl, 3);

            //frameSize * 3 as there are 3 floats for every particle
            bufferSize = FluidSettings.instance.frameSize * 3;

            OpenMemoryMap(memMapName, bufferSize);

            OpenMemoryMap(memMapNameVel, bufferSize);




            Debug.Log("Fluid Simulation Initialized");


            //var dirInfo = System.IO.Directory.CreateDirectory(Application.dataPath + "/../" + CrowdExperiment.instance.instanceName.Split('.')[0] + "_" + Settings.simIndex + "_" + Settings.FluidExpName.Split('.')[0]);


            CellToParticles = new NativeMultiHashMap<int3, int>(FluidSettings.instance.frameSize * Clones + ((CrowdExperiment.instance.TerrainX) / 2 * (CrowdExperiment.instance.TerrainZ) / 2), Allocator.Persistent);
            //Debug.Log(CellToParticles.Capacity);

            FluidPos = new NativeList<float3>(FluidSettings.instance.frameSize * Clones, Allocator.Persistent);
            FluidVel = new NativeList<float3>(FluidSettings.instance.frameSize * Clones, Allocator.Persistent);

            //dataPath = Application.dataPath + "/../" + CrowdExperiment.instance.instanceName.Split('.')[0] + "_" + Settings.simIndex + "_" + Settings.FluidExpName.Split('.')[0];
            dataPath = ExperimentManager.instance.Directory + "/Fluid";

        }

        protected override void OnStopRunning()
        {
            FluidPos.Dispose();
            FluidVel.Dispose();
            CellToParticles.Dispose();
            AcessDLL.CloseMemoryShare(memMapControl);
            AcessDLL.CloseMemoryShare(memMapName);
            AcessDLL.CloseMemoryShare(memMapNameVel);
        }






        #endregion



        private void FillFrameParticlePositions()
        {
            var settings = FluidSettings.instance;

            float[] floatStreamVel = new float[bufferSize];
            AcessDLL.ReadMemoryShare(memMapNameVel, floatStreamVel);

            float[] floatStream = new float[bufferSize];
            AcessDLL.ReadMemoryShare(memMapName, floatStream);


            for (int i = 0; i < floatStream.Length - 2; i += 3)
            {

                float xv = floatStreamVel[i];
                float yv = floatStreamVel[i + 1];
                float zv = -floatStreamVel[i + 2];

                //if (xv == 0 && yv == 0 & zv == 0) continue;


                float x = floatStream[i];
                float y = floatStream[i + 1];
                float z = -floatStream[i + 2];

                if (x == 0 && y == 0 & z == 0) continue;

                float3 pos = new float3(x, y, z) * settings.scale + settings.translate;
                FluidPos.Add(pos);

                for (int l = 1; l < Clones; l++)
                {
                    float xl = UnityEngine.Random.Range(FluidSettings.instance.particleRadius, FluidSettings.instance.particleRadius * 2);
                    float yl = UnityEngine.Random.Range(FluidSettings.instance.particleRadius, FluidSettings.instance.particleRadius * 2);
                    float zl = UnityEngine.Random.Range(FluidSettings.instance.particleRadius, FluidSettings.instance.particleRadius * 2);
                    float3 offset = new float3(xl, yl, zl);
                    FluidPos.Add(pos + offset);
                }


                float3 vel = new float3(xv, yv, zv);
                FluidVel.Add(vel);


                for (int l = 1; l < Clones; l++)
                {
                    //float xl = UnityEngine.Random.Range(0f, FluidSettings.instance.particleRadius);
                    //float yl = UnityEngine.Random.Range(0f, FluidSettings.instance.particleRadius);
                    //float zl = UnityEngine.Random.Range(0f, FluidSettings.instance.particleRadius);
                    //float3 offset = new float3(xl, yl, zl);
                    FluidVel.Add(vel);
                }



                //int3 cube = new int3((int)math.floor(pos.x / (stride * 100f)), (int)math.floor(pos.y / (stride * 10f)), (int)math.floor(pos.z / (stride * 50f)));
                //if (marchingCubes.TryGetValue(cube, out List<float3> values))
                //{
                //    values.Add(pos);
                //}
                //else
                //{
                //    //if(i < 200)
                //    //{
                //    //    Debug.Log(cube + " " + pos);
                //    //}
                //    marchingCubes.Add(cube, new List<float3> { pos });
                //}






            }




            //for (int i = 0; i < floatStreamVel.Length - 2; i += 3)
            //{

            //    if (x == 0 && y == 0 & z == 0) continue;




            //}


        }


        private int lastSyncFrame = 0;

        private bool WaitForFluidSim()
        {

            float[] ControlData = new float[3];
            AcessDLL.ReadMemoryShare(memMapControl, ControlData);
            timeSplish = ControlData[0];


            ControlData[1] = lastSyncFrame / CrowdExperiment.instance.FramesPerSecond;


            if (ControlData[0] > ControlVariables.instance.SimThreshold)
            {
                AcessDLL.CloseMemoryShare(memMapControl);
                AcessDLL.CloseMemoryShare(memMapName);
                AcessDLL.CloseMemoryShare(memMapNameVel);

                FluidLogger.WriteToFile(dataPath + "/log.txt");

                Application.Quit();
            }

            AcessDLL.WriteMemoryShare(memMapControl, ControlData);
            //Debug.Log(ControlData[0]);

            if (ControlData[1] > ControlData[0] && ControlVariables.instance.SyncWithFluidSimulator)
            {
                ControlVariables.instance.LockBioCrowds = true;
                return true;
            }
            else
            {
                lastSyncFrame = frame;
                ControlVariables.instance.LockBioCrowds = false;
                return false;

            }
        }


        public string dataPath;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //HACK: Write better sync between fluid sim and biocrowds
            // Actually giving control back to unity is hard to do here....
            WaitForFluidSim();

            FluidPos.Clear();
            FluidVel.Clear();

            FillFrameParticlePositions();

            //Debug.Log(frame + " FluidVel Size: " + FluidVel.Length + " " + FluidVel.Capacity + " FluidPos Size: " + FluidPos.Length + " " + FluidPos.Capacity + " CellToParticles Size: " + CellToParticles.Length + " " + CellToParticles.Capacity);


            for (int i = 0; i < cellGroup.Length; i++)
            {
                int3 key = cellGroup.CellName[i].Value;

                CellToParticles.Remove(key);
            }


            var FillCellMomentaJob = new FillCellFluidParticles
            {
                CellToParticles = CellToParticles.ToConcurrent(),
                FluidPos = FluidPos
            };

            var FillCellJobHandle = FillCellMomentaJob.Schedule(FluidPos.Length, SimulationConstants.instance.BatchSize, inputDeps);

            FillCellJobHandle.Complete();


            if (frame % 300 == 0)
            {
                CellToParticles.Clear();
            }




            ScreenCapture.CaptureScreenshot(dataPath + "/frame" + frame.ToString().PadLeft(8, '0') + ".png");




            //DebugFluid();
            //DrawFluid();
            frame++;

            //Debug.Log(frame);
            return inputDeps;
        }



        private void DebugFluid()
        {
            for (int i = 0; i < FluidPos.Length; i++)
            {
                float magnitude = ((Vector3)FluidVel[i]).magnitude / 50f;
                Color c = Color.LerpUnclamped(Color.yellow, Color.red, magnitude);

                Debug.DrawRay(FluidPos[i], FluidVel[i]);
            }

        }




        private void OpenMemoryMap(string mapName, int size)
        {
            int map = AcessDLL.OpenMemoryShare(mapName, size);
            switch (map)
            {
                case 0:
                    Debug.Log("Memory Map " + mapName + " Created");
                    break;
                case -1:
                    Debug.Log("Memory Map " + mapName + " Array too large");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;

                case -2:
                    Debug.Log("Memory Map " + mapName + " could not create file mapping object");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;
                case -3:
                    Debug.Log("Memory Map " + mapName + " could not create map view of the file");
                    this.Enabled = false;
                    World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                    //TODO: disable every fluid sim system
                    return;
                default:
                    Debug.Log("A Memory Map " + mapName + " Already Exists");
                    break;
            }
        }

    }


    [UpdateAfter(typeof(FluidInitializationSystem))]
    [UpdateAfter(typeof(FluidInitializationSystem))]
    [UpdateBefore(typeof(FluidParticleToCell))]
    public class FluidBarrier : BarrierSystem { }

    [UpdateAfter(typeof(SpawnAgentBarrier))]
    [UpdateBefore(typeof(FluidParticleToCell))]
    public class FluidInitializationSystem : JobComponentSystem
    {
        [Inject] public AgentSpawner agentSpawner;
        [Inject] public FluidBarrier spawnerBarrier;

        NativeArray<CubeObstacleData> data;
        public static EntityArchetype AgentArchetype;
        public static MeshInstanceRenderer AgentRenderer;

        public NativeArray<int> AgentAtObstacle;
        public int LastIDUsed;

        public static FluidSettings settings;


        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public ComponentDataArray<AgentStep> AgentStep;
            [ReadOnly] public EntityArray entities;
            [ReadOnly] public readonly int Length;
        }
        [Inject] AgentGroup agentGroup;

        public struct AddFluidData : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public EntityArray entities;

            public void Execute(int index)
            {

                System.Random r = new System.Random(index);
                float tau = (float)r.NextDouble() * 0.4f + 0.4f;
                if (!settings.randTau)
                {
                    tau = settings.initialTau;
                }


                float mass = (float)r.NextDouble() * 60f + 40f;
                if (!settings.randMass)
                {
                    mass = 65f;
                }

                CommandBuffer.AddComponent<FluidData>(index, entities[index], new FluidData { tau = tau });
                CommandBuffer.AddComponent<PhysicalData>(index, entities[index], new PhysicalData { mass = mass });
                CommandBuffer.AddComponent<CouplingComponent>(index, entities[index], new CouplingComponent { CouplingDistance = 3f, CurrentCouplings = 0, MaxCouplings = 2 });
                CommandBuffer.AddComponent<SurvivalComponent>(index, entities[index], new SurvivalComponent { threshold = 0.5f, survival_state = 0 });

                //Survival Buffer
                CommandBuffer.AddBuffer<DotElement>(index, entities[index]);

                //End Survival Buffer
            }
        }

        public struct SpawnFluidObstacles : IJobParallelFor
        {
            public NativeArray<CubeObstacleData> data;
            public EntityCommandBuffer.Concurrent CommandBuffer;

            [ReadOnly] public NativeArray<int> AgentAtCellQuantity;
            [ReadOnly] public int LastIDUsed;



            public void Execute(int index)
            {
                //TODO: Make cube size by dimension
                float halfCubeSizeX = (1f * settings.scale.x) / 2f;
                float halfCubeSizeZ = (1f * settings.scale.z) / 2f;

                float xi = (data[index].position.x * settings.scale.x + settings.translate.x);
                float zi = (data[index].position.z * settings.scale.z + settings.translate.z);

                float maxX = xi + halfCubeSizeX;
                float maxZ = zi + halfCubeSizeZ;

                float minX = xi - halfCubeSizeX;
                float minZ = zi - halfCubeSizeZ;

                int i = AgentAtCellQuantity[index] + LastIDUsed;

                for (float x = minX; x < maxX; x++)
                {
                    for (float z = minZ; z < maxZ; z++)
                    {

                        if (x > minX + 1 && z > minZ + 1 && z < maxZ - 2 && x < maxX - 2)
                        {
                            continue;
                        }

                        float3 pos = new float3(x, 0f, z);
                        float3 g = pos;
                        CommandBuffer.CreateEntity(index, AgentArchetype);
                        CommandBuffer.SetComponent(index, new Position { Value = pos });
                        CommandBuffer.SetComponent(index, new AgentData
                        {
                            ID = i,
                            MaxSpeed = 0f,
                            Radius = 1f
                        });
                        CommandBuffer.SetComponent(index, new AgentGoal { SubGoal = g, EndGoal = g });
                        CommandBuffer.AddComponent(index, new CouplingComponent { CouplingDistance = 1f, CurrentCouplings = 0, MaxCouplings = 7 });

                        CommandBuffer.AddSharedComponent(index, AgentRenderer);

                        i++;
                    }
                }
            }
        }


        protected override void OnStartRunning()
        {
            if (!FluidSettings.instance.Enabled)
            {
                this.Enabled = false;
                World.Active.GetExistingManager<FluidParticleToCell>().Enabled = false;
                World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                World.Active.GetExistingManager<FluidBarrier>().Enabled = false;
                return;
            }


            var entityManager = World.Active.GetOrCreateManager<EntityManager>();


            AgentArchetype = entityManager.CreateArchetype(
               ComponentType.Create<Position>(),
               ComponentType.Create<Rotation>(),
               ComponentType.Create<AgentData>(),
               ComponentType.Create<AgentGoal>(),
               ComponentType.Create<Counter>());

            settings = FluidSettings.instance;
            data = new NativeArray<CubeObstacleData>(GetObstacleData(), Allocator.Persistent);

            AgentRenderer = BioCrowdsBootStrap.GetLookFromPrototype("ObstacleRenderer");
            AgentAtObstacle = new NativeArray<int>(data.Length, Allocator.Persistent);
            LastIDUsed = agentSpawner.lastAgentId;
        }

        protected override void OnStopRunning()
        {
            data.Dispose();
            AgentAtObstacle.Dispose();
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBuffer = spawnerBarrier.CreateCommandBuffer();


            if (data.Length > 0)
            {
                //HACK: Get cube size from .obj
                //This is in meters
                //TODO: Make cube size by dimension
                float halfCubeSizeX = (1f * settings.scale.x) / 2f;
                float halfCubeSizeZ = (1f * settings.scale.z) / 2f;

                float x = (data[0].position.x * settings.scale.x + settings.translate.x);
                float z = (data[0].position.z * settings.scale.z + settings.translate.z);

                float maxX = x + halfCubeSizeX;
                float maxZ = z + halfCubeSizeZ;

                float minX = x - halfCubeSizeX;
                float minZ = z - halfCubeSizeZ;



                int lastValue = (int)(math.floor(halfCubeSizeX * 2) * math.floor(halfCubeSizeZ * 2));
                AgentAtObstacle[0] = 0;
                for (int i = 1; i < data.Length; i++)
                {
                    halfCubeSizeX = (1f * settings.scale.x) / 2f;
                    halfCubeSizeZ = (1f * settings.scale.z) / 2f;

                    x = (data[i].position.x * settings.scale.x + settings.translate.x);
                    z = (data[i].position.z * settings.scale.z + settings.translate.z);

                    maxX = x + halfCubeSizeX;
                    maxZ = z + halfCubeSizeZ;

                    minX = x - halfCubeSizeX;
                    minZ = z - halfCubeSizeZ;


                    AgentAtObstacle[i] = lastValue + AgentAtObstacle[i - 1];
                    CubeObstacleData spawnList = data[i - 1];
                    lastValue = (int)(math.floor(halfCubeSizeX * 2) * math.floor(halfCubeSizeZ * 2));

                }

                var SpawnFluidObstaclesJob = new SpawnFluidObstacles
                {
                    AgentAtCellQuantity = AgentAtObstacle,
                    CommandBuffer = commandBuffer.ToConcurrent(),
                    LastIDUsed = LastIDUsed,
                    data = data
                };
                var SpawnFluidObstaclesJobHandle = SpawnFluidObstaclesJob.Schedule(data.Length, SimulationConstants.instance.BatchSize, inputDeps);
                SpawnFluidObstaclesJobHandle.Complete();
            }





            var FluidDataJob = new AddFluidData
            {
                CommandBuffer = commandBuffer.ToConcurrent(),
                entities = agentGroup.entities
            };
            var FluidDataHandle = FluidDataJob.Schedule(agentGroup.Length, SimulationConstants.instance.BatchSize, inputDeps);
            FluidDataHandle.Complete();






            this.Enabled = false;
            return FluidDataHandle;

        }

        private CubeObstacleData[] GetObstacleData()
        {
            return FluidSettings.instance.cubeObstacleDatas;

        }
    }

    [UpdateAfter(typeof(FluidInitializationSystem))]
    [UpdateBefore(typeof(FluidMovementOnAgent))]
    public class AgentMassMapSystem : JobComponentSystem
    {

        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<PhysicalData> PhysicalData;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [ReadOnly] public readonly int Length;
        }



        public NativeHashMap<int, float> AgentID2MassMap;
        [Inject] AgentGroup m_agentGroup;

        public struct FillMassMapJob : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<PhysicalData> PhysicalData;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
            [WriteOnly] public NativeHashMap<int, float>.Concurrent AgentMassMap;


            public void Execute(int index)
            {
                AgentMassMap.TryAdd(AgentData[index].ID, PhysicalData[index].mass);
            }
        }


        protected override void OnStartRunning()
        {
            AgentID2MassMap = new NativeHashMap<int, float>(0, Allocator.Persistent);
            Debug.Log("MapCreated");
        }

        protected override void OnStopRunning()
        {
            AgentID2MassMap.Dispose();

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (AgentID2MassMap.Length != m_agentGroup.Length)
            {
                Debug.Log("Map Is Created");
                AgentID2MassMap.Dispose();
                AgentID2MassMap = new NativeHashMap<int, float>(m_agentGroup.Length, Allocator.Persistent);

                var massJob = new FillMassMapJob()
                {
                    AgentData = m_agentGroup.AgentData,
                    AgentMassMap = AgentID2MassMap.ToConcurrent(),
                    PhysicalData = m_agentGroup.PhysicalData
                };

                var massjob_handle = massJob.Schedule(m_agentGroup.Length, SimulationConstants.instance.BatchSize);
                massjob_handle.Complete();
                return massjob_handle;

            }



            return inputDeps;


        }
    }


    public static class AcessDLL
    {

        public static string dataPath = "out.txt";

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

