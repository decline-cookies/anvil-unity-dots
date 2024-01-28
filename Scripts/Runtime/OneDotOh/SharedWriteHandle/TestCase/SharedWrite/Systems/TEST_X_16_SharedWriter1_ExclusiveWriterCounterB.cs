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
    [UpdateAfter(typeof(TEST_X_15_SharedWriter0_ExclusiveWriterCounterA))]
    [BurstCompile]
    public partial struct TEST_X_16_SharedWriter1_ExclusiveWriterCounterB : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_X_16_SharedWriter1_ExclusiveWriterCounterB)}");

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SWTCBufferX>();
            state.RequireForUpdate<SWTCExclusiveCounterB>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // state.GetSystemDependency(out JobHandle dependsOn);
            // Debug.Log("TEST_X_16_SharedWriter1_ExclusiveWriterCounterB");
            state.AcquireSharedWriteHandle<SWTCBufferX>(out JobHandle sharedWriteHandle,
                                                        out SharedWriteTrigger sharedWriteTrigger);
            Debug.Assert(sharedWriteTrigger == SharedWriteTrigger.Inline);

            SWTCBufferX swtcBufferX = SystemAPI.GetSingleton<SWTCBufferX>();
            SWTCExclusiveCounterB swtcExclusiveCounterB = SystemAPI.GetSingletonRW<SWTCExclusiveCounterB>().ValueRW;

            SharedWriteExclusiveCounterJob job = new SharedWriteExclusiveCounterJob(
                                                                                    ref swtcBufferX,
                                                                                    ref swtcExclusiveCounterB,
                                                                                    1,
                                                                                    343,
                                                                                    s_ProfilerMarker);

            //NOTE: Dropping the incoming dependency
            JobHandle dependsOn = sharedWriteHandle;

            dependsOn = job.ScheduleByRef(dependsOn);

            state.ReleaseSharedWriteHandle<SWTCBufferX>(ref dependsOn);

            //NOTE: Must recombine the incoming dependency since we went out of band.
            state.SetSystemDependency(dependsOn);
        }

        [BurstCompile]
        private struct SharedWriteExclusiveCounterJob : IJob
        {
            [NativeDisableContainerSafetyRestriction]
            private SWTCBufferX m_BufferX;

            private SWTCExclusiveCounterB m_ExclusiveCounterB;

            private readonly int m_Index;
            private readonly int m_Value;
            private readonly ProfilerMarker m_ProfilerMarker;

            public SharedWriteExclusiveCounterJob(
                ref SWTCBufferX bufferX,
                ref SWTCExclusiveCounterB exclusiveCounterB,
                int index,
                int value,
                ProfilerMarker profilerMarker)
            {
                m_BufferX = bufferX;
                m_ExclusiveCounterB = exclusiveCounterB;
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

                ref int counter = ref m_ExclusiveCounterB.Buffer.ElementAt(0);
                counter = 21;

                m_ProfilerMarker.End();
            }
        }
    }
#endif
}