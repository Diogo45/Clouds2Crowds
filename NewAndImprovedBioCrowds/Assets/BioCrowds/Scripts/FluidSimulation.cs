
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
            [ReadOnly]  public ComponentDataArray<PhysicalData> PhysicalData;
            [ReadOnly]  public ComponentDataArray<AgentData> AgentData;
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

                var massjob_handle = massJob.Schedule(m_agentGroup.Length, Settings.BatchSize);
                massjob_handle.Complete();
                return massjob_handle;

            }



            return inputDeps;


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

        [Inject] public FluidBarrier spawnerBarrier;

        NativeArray<CubeObstacleData> data; 


        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
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

                CommandBuffer.AddComponent<FluidData>(index, entities[index], new FluidData { tau = tau });
                CommandBuffer.AddComponent<PhysicalData>(index, entities[index], new PhysicalData { mass = 70f });
                CommandBuffer.AddComponent<CouplingComponent>(index, entities[index], new CouplingComponent { CouplingDistance = 1f, CurrentCouplings = 0, MaxCouplings = 2 });


            }
        }

        public struct SpawnFluidObstacles : IJobParallelFor
        {
            public void Execute(int index)
            {
                throw new NotImplementedException();
            }
        }


        protected override void OnCreateManager()
        {
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();



            var AgentArchetype = entityManager.CreateArchetype(
               ComponentType.Create<Position>(),
               ComponentType.Create<Rotation>(),
               ComponentType.Create<AgentData>(),
               ComponentType.Create<AgentGoal>(),
               ComponentType.Create<Counter>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            data = new NativeArray<CubeObstacleData>(GetObstacleData(), Allocator.Persistent);


            var FluidDataJob = new AddFluidData
            {
                CommandBuffer = spawnerBarrier.CreateCommandBuffer().ToConcurrent(),
                entities = agentGroup.entities
            };





            var FluidDataHandle = FluidDataJob.Schedule(agentGroup.Length, Settings.BatchSize, inputDeps);

            FluidDataHandle.Complete();

            this.Enabled = false;
            return inputDeps;

        }

        private CubeObstacleData[] GetObstacleData()
        {
            return Settings.instance.getFluid().cubeObstacleDatas;

        }
    }


    [UpdateAfter(typeof(FluidParticleToCell)), UpdateAfter(typeof(AgentMassMapSystem))]
    [UpdateBefore(typeof(AgentMovementSystem))]
    public class FluidMovementOnAgent : JobComponentSystem
    {

        #region VARIABLES
        [Inject] FluidParticleToCell m_fluidParticleToCell;
        public NativeHashMap<int3, float3> CellMomenta;
        public NativeHashMap<int3, float3> ParticleSetMass;

        private static float thresholdDist = 0.01f;
        //1 g/cm3 = 1000 kg/m3
        //Calculate based on the original SplishSplash code, mass = volume * density
        //Where density = 1000kg/m^3 and volume = 0.8 * particleDiameter^3
        private static float particleMass = 0.0001f;//kg
        private static float agentMass = 65f; //TODO USAR MASSA COMPONENTAL
        private static float timeStep = 1f / Settings.experiment.FramesPerSecond;
        private float particleRadius = 0.025f;

        //0 --> Total inelastic collision
        //1 --> Elastic Collision
        private static float RestitutionCoef = 0f;




        public struct AgentGroup
        {
            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            [ReadOnly] public ComponentDataArray<AgentData> AgentData;
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
        #endregion

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

                while (CellToParticles.TryGetNextValue(out particleID, ref it))
                {
                    vel = FluidVel[particleID] / (timeStep);

                    P = vel * particleMass;
                    M_r += P;
                    numPart++;
                }
                ParticleSetMass.TryAdd(key, numPart * particleMass);
                CellMomenta.TryAdd(key, M_r);

            }
        }

        struct ApplyFluidMomentaOnAgents : IJobParallelFor
        {

            [ReadOnly] public NativeHashMap<int3, float3> CellMomenta;
            [ReadOnly] public NativeHashMap<int3, float3> ParticleSetMass;
            [ReadOnly] public ComponentDataArray<FluidData> FluidData;

            [ReadOnly] public ComponentDataArray<Position> AgentPos;
            public ComponentDataArray<AgentStep> AgentStep;

            [ReadOnly] public float RestitutionCoef;

            public void Execute(int index)
            {
                int3 cell = new int3((int)math.floor(AgentPos[index].Value.x / 2.0f) * 2 + 1, 0,
                                   (int)math.floor(AgentPos[index].Value.z / 2.0f) * 2 + 1);



                bool keepgoing = CellMomenta.TryGetValue(cell, out float3 particleVel);
                if (!keepgoing) return;

                keepgoing = ParticleSetMass.TryGetValue(cell, out float3 particleSetMass);
                if (!keepgoing) return;

                float3 OldAgentVel = AgentStep[index].delta;
                //tau is how much the fluid acts on the agent, or (1 - tau) is how much the agent resists the fluid
                float tau = FluidData[index].tau;
                //TOTAL INELASTIC COLLISION
                OldAgentVel = (OldAgentVel * agentMass + tau * particleVel) / (agentMass + tau * particleSetMass);

                //HACK: For now, while we dont have ragdolls or the buoyancy(upthrust) force, not making use of the y coordinate 
                OldAgentVel.y = 0f;

                AgentStep[index] = new AgentStep { delta = OldAgentVel };



            }
        }

        #region ON...
        protected override void OnStartRunning()
        {

            CellMomenta = new NativeHashMap<int3, float3>((Settings.experiment.TerrainX * Settings.experiment.TerrainZ) / 4, Allocator.Persistent);
            ParticleSetMass = new NativeHashMap<int3, float3>((Settings.experiment.TerrainX * Settings.experiment.TerrainZ) / 4, Allocator.Persistent);

            //1 g/cm3 = 1000 kg/m3
            //Calculate based on the original SplishSplash code, mass = volume * density
            //Where density = 1000kg/m^3 and volume = 0.8 * particleDiameter^3
            float particleDiameter = 2 * particleRadius * 10f;
            float volume = 0.8f * math.pow(particleDiameter, 3);
            float density = 1000f;
            //particleMass = volume * density;
            particleMass = 0.001f;
            Debug.Log("Particle Mass: " + particleMass);

        }

        protected override void OnDestroyManager()
        {
            CellMomenta.Dispose();
            ParticleSetMass.Dispose();
        }
        #endregion

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            WriteData();

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


            //DrawMomenta();

            ApplyFluidMomentaOnAgents applyMomenta = new ApplyFluidMomentaOnAgents
            {
                AgentPos = agentGroup.AgentPos,
                AgentStep = agentGroup.AgentStep,
                CellMomenta = CellMomenta,
                RestitutionCoef = RestitutionCoef,
                ParticleSetMass = ParticleSetMass,
                FluidData = agentGroup.FluidData

            };

            var applyMomentaHandle = applyMomenta.Schedule(agentGroup.Length, Settings.BatchSize, momentaJobHandle);

            applyMomentaHandle.Complete();




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
        public ComputeBuffer fluidBuffer;
        public NativeList<float3> FluidPos;
        public NativeList<float3> FluidVel;
        public NativeMultiHashMap<int3, int> CellToParticles;

        public Dictionary<int3, List<float3>> marchingCubes;


        public int frameSize = 100000;//particles
        public int bufferSize;// number of particles times the number of floats of data of each particle, for now 3 for 3 floats

        public int frame = 0;
        private int last = 0;
        private bool first = true;
        private static float thresholdHeigth = 1.7f;
        private const string memMapName = "unityMemMap";
        private const string memMapNameVel = "unityMemMapVel";
        //[timeSPH, timeBC, frameSize]
        private const string memMapControl = "unityMemMapControl";

        public int numPointsPerAxis = 30;
        public float stride = 1 / 30f;
        private bool sync = true;       //turn of for performance

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
                if (ppos.y > thresholdHeigth ||
                    ppos.x > Settings.experiment.TerrainX || ppos.z > Settings.experiment.TerrainZ ||
                    ppos.x < 0f || ppos.z < 0f) return;

                int3 cell = new int3((int)math.floor(FluidPos[index].x / 2.0f) * 2 + 1, 0,
                                     (int)math.floor(FluidPos[index].z / 2.0f) * 2 + 1);


                CellToParticles.Add(cell, index);


            }
        }



        #region ON...
        protected override void OnStartRunning()
        {
            if (!Settings.experiment.FluidSim)
            {
                Debug.Log(Settings.experiment.FluidSim);
                this.Enabled = false;
                World.Active.GetExistingManager<FluidMovementOnAgent>().Enabled = false;
                return;
            }


            fluidBuffer = GameObject.FindObjectOfType<MeshGenerator>().pointsBuffer;
            marchingCubes = new Dictionary<int3, List<float3>>();
            numPointsPerAxis = GameObject.FindObjectOfType<MeshGenerator>().numPointsPerAxis;
            stride = 1f / numPointsPerAxis;

            CellToParticles = new NativeMultiHashMap<int3, int>(frameSize + ((Settings.experiment.TerrainX) / 2 * (Settings.experiment.TerrainZ) / 2), Allocator.Persistent);
            //Debug.Log(CellToParticles.Capacity);

            FluidPos = new NativeList<float3>(frameSize, Allocator.Persistent);
            FluidVel = new NativeList<float3>(frameSize, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            FluidPos.Dispose();
            FluidVel.Dispose();
            CellToParticles.Dispose();

        }


        protected override void OnCreateManager()
        {

            OpenMemoryMap(memMapControl, 3);

            //frameSize * 3 as there are 3 floats for every particle
            bufferSize = frameSize * 3;
            //TODO: Get frameSize from FluidSimulator

            OpenMemoryMap(memMapName, bufferSize);

            OpenMemoryMap(memMapNameVel, bufferSize);


            Debug.Log("Fluid Simulation Initialized");
        }




        #endregion



        private void FillFrameParticlePositions()
        {
            var settings = Settings.instance.getFluid();

            float[] floatStreamVel = new float[bufferSize];
            AcessDLL.ReadMemoryShare(memMapNameVel, floatStreamVel);

            float[] floatStream = new float[bufferSize];
            AcessDLL.ReadMemoryShare(memMapName, floatStream);

            if (floatStream.Length != floatStreamVel.Length) Debug.Log("WTF");

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
                //TODO: Parametrize the translation and scale]
                float3 pos = new float3(x, y, z) * settings.scale + settings.translate;
                FluidPos.Add(pos);

                //TODO: Parametrize the translation and scale]
                float3 vel = new float3(xv, yv, zv);
                FluidVel.Add(vel);


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


                //for (int l= 1; l < NLerp; l++)
                //{
                //    float xl = UnityEngine.Random.Range(0f, 0.5f);
                //    float yl = UnityEngine.Random.Range(0f, 0.5f);
                //    float zl = UnityEngine.Random.Range(0f, 0.5f);
                //    float3 offset = new float3(xl,yl,zl);
                //    FluidPos.Add(pos + offset);
                //}



            }


            //for (int i = 0; i < floatStreamVel.Length - 2; i += 3)
            //{

            //    if (x == 0 && y == 0 & z == 0) continue;


            //    for (int l = 1; l < NLerp; l++)
            //    {
            //        float xl = UnityEngine.Random.Range(0f, 0.5f);
            //        float yl = UnityEngine.Random.Range(0f, 0.5f);
            //        float zl = UnityEngine.Random.Range(0f, 0.5f);
            //        float3 offset = new float3(xl, yl, zl);
            //        FluidVel.Add(vel + offset);
            //    }

            //}


        }


        private bool WaitForFluidSim()
        {

            float[] ControlData = new float[3];
            AcessDLL.ReadMemoryShare(memMapControl, ControlData);

            ControlData[1] = frame / Settings.experiment.FramesPerSecond;
            AcessDLL.WriteMemoryShare(memMapControl, ControlData);
            //Debug.Log(ControlData[0]);

            if (ControlData[1] > ControlData[0] && sync)
            {
                Thread.Sleep(1);
                return true;
            }

            //Debug.Log(frame + " " + ControlData[0] + " " + ControlData[1]);

            return false;
        }





        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //HACK: Write better sync between fluid sim and biocrowds
            while (WaitForFluidSim()) { }

            FluidPos.Clear();
            FluidVel.Clear();

            FillFrameParticlePositions();

            Debug.Log(frame + " FluidVel Size: " + FluidVel.Length + " " + FluidVel.Capacity + " FluidPos Size: " + FluidPos.Length + " " + FluidPos.Capacity + " CellToParticles Size: " + CellToParticles.Length + " " + CellToParticles.Capacity);


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

            var FillCellJobHandle = FillCellMomentaJob.Schedule(FluidPos.Length, Settings.BatchSize, inputDeps);

            FillCellJobHandle.Complete();


            if (frame % 300 == 0)
            {
                CellToParticles.Clear();
            }



            string s = frame.ToString();
            if (s.Length == 1) ScreenCapture.CaptureScreenshot(Application.dataPath + "/../Prints/frame0000" + frame + ".png");
            if (s.Length == 2) ScreenCapture.CaptureScreenshot(Application.dataPath + "/../Prints/frame000" + frame + ".png");
            if (s.Length == 3) ScreenCapture.CaptureScreenshot(Application.dataPath + "/../Prints/frame00" + frame + ".png");
            if (s.Length == 4) ScreenCapture.CaptureScreenshot(Application.dataPath + "/../Prints/frame0" + frame + ".png");
            if (s.Length == 5) ScreenCapture.CaptureScreenshot(Application.dataPath + "/../Prints/frame" + frame + ".png");



            //DebugFluid();
            //DrawFluid();
            frame++;

            //Debug.Log(frame);
            return inputDeps;
        }

        int indexFromCoord(int x, int y, int z)
        {
            return z * numPointsPerAxis * numPointsPerAxis + y * numPointsPerAxis + x;
        }

        private void DrawFluid()
        {
            float4[] fluidData = new float4[(numPointsPerAxis * numPointsPerAxis * numPointsPerAxis)];
            foreach (int3 key in marchingCubes.Keys)
            {
                List<float3> particles = marchingCubes[key];

                //int3 cube = new int3((int)math.ceil(pos.x / (stride * 100f)), (int)math.ceil(pos.y / (stride * 10f)), (int)math.ceil(pos.z / (stride * 27f)));
                float3 pos = new float3(key.x * (stride * 100f) + (stride / 2), key.y * (stride * 10f) + (stride / 2), key.z * (stride * 50f) + (stride / 2));

                int normalX = key.x;//(int)math.floor((key.x /*- translate.x*/) / (stride * 100f));
                int normalY = key.y; //(int)math.floor((key.y /*- translate.y*/) / (stride * 10f));
                int normalZ = key.z;//(int)math.floor((key.z /*- translate.z*/) / (stride * 27f));

                int3 normalizedCube = new int3(normalX, normalY, normalZ);

                int index = indexFromCoord(normalizedCube.x, normalizedCube.y, normalizedCube.z);
                if (index > fluidData.Length || index < 0)
                {
                    Debug.Log(index + " " + key + " " + normalizedCube + " " + pos);
                }
                fluidData[index] = new float4(pos, 1f);
            }

            fluidBuffer.SetData(fluidData);

        }

        private void DebugFluid()
        {
            int j = 0;
            for (int i = 0; i < FluidPos.Length; i++)
            {
                if (i % 1000 == 0)
                {
                    Debug.Log(FluidPos[i] + " " + FluidPos[i] + FluidVel[i] / 75f);
                }
                float magnitude = ((Vector3)FluidVel[i]).magnitude / 50f;
                Color c = Color.LerpUnclamped(Color.yellow, Color.red, magnitude);

                Debug.DrawLine(FluidPos[i], FluidPos[i] + FluidVel[i] / 75f);
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

