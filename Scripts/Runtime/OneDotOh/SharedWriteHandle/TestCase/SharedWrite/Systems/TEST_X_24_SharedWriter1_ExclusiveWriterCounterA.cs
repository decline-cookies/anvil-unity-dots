using Anvil.CSharp.Mathematics;
using Anvil.Unity.DOTS.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_X_23_SharedWriter0_ExclusiveWriterCounterA))]
    [BurstCompile]
    public partial struct TEST_X_24_SharedWriter1_ExclusiveWriterCounterA : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_X_24_SharedWriter1_ExclusiveWriterCounterA)}");

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SWTCBufferX>();
            state.RequireForUpdate<SWTCExclusiveCounterA>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // state.GetSystemDependency(out JobHandle dependsOn);
            // Debug.Log("TEST_X_24_SharedWriter1_ExclusiveWriterCounterA");
            state.AcquireSharedWriteHandle<SWTCBufferX>(out JobHandle sharedWriteHandle,
                                                        out SharedWriteTrigger sharedWriteTrigger);
            Debug.Assert(sharedWriteTrigger == SharedWriteTrigger.New);

            SWTCBufferX swtcBufferX = SystemAPI.GetSingleton<SWTCBufferX>();
            SWTCExclusiveCounterA swtcExclusiveCounterA = SystemAPI.GetSingletonRW<SWTCExclusiveCounterA>().ValueRW;

            SharedWriteExclusiveCounterJob job = new SharedWriteExclusiveCounterJob(
                                                                                    ref swtcBufferX,
                                                                                    ref swtcExclusiveCounterA,
                                                                                    1,
                                                                                    654,
                                                                                    s_ProfilerMarker);

            //NOTE: Because we are new, we are combining with the incoming dependency
            JobHandle dependsOn = sharedWriteHandle;

            dependsOn = job.ScheduleByRef(dependsOn);
            // dependsOn = JobHandle.CombineDependencies(dependsOn, state.Dependency);

            state.ReleaseSharedWriteHandle<SWTCBufferX>(ref dependsOn);

            state.SetSystemDependency(dependsOn);
        }

        [BurstCompile]
        private struct SharedWriteExclusiveCounterJob : IJob
        {
            [NativeDisableContainerSafetyRestriction]
            private SWTCBufferX m_BufferX;

            private SWTCExclusiveCounterA m_ExclusiveCounterA;

            private readonly int m_Index;
            private readonly int m_Value;
            private readonly ProfilerMarker m_ProfilerMarker;

            public SharedWriteExclusiveCounterJob(
                ref SWTCBufferX bufferX,
                ref SWTCExclusiveCounterA exclusiveCounterA,
                int index,
                int value,
                ProfilerMarker profilerMarker)
            {
                m_BufferX = bufferX;
                m_ExclusiveCounterA = exclusiveCounterA;
                m_Index = index;
                m_Value = value;
                m_ProfilerMarker = profilerMarker;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                MathUtil.FindPrimeNumber(SWTCConstants.NTH_PRIME_VALUE_TO_FIND);
                ref int element = ref m_BufferX.Buffer.ElementAt(m_Index);
                element = m_Value;

                ref int counter = ref m_ExclusiveCounterA.Buffer.ElementAt(0);
                counter = 373;

                m_ProfilerMarker.End();
            }
        }
    }
#endif
}