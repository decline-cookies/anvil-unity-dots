using Anvil.CSharp.Mathematics;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_X_26_SharedReader1))]
    [BurstCompile]
    public partial struct TEST_X_27_SharedReaderCounterA : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_X_27_SharedReaderCounterA)}");

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SWTCExclusiveCounterA>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.GetSystemDependency(out JobHandle dependsOn);

            SWTCExclusiveCounterA swtcExclusiveCounterA = SystemAPI.GetSingleton<SWTCExclusiveCounterA>();

            SharedReadCounterJob job = new SharedReadCounterJob(
                                                                                    ref swtcExclusiveCounterA,
                                                                                    373,
                                                                                    s_ProfilerMarker);

            dependsOn = job.ScheduleByRef(dependsOn);

            state.SetSystemDependency(dependsOn);
        }

        [BurstCompile]
        private struct SharedReadCounterJob : IJob
        {
            [ReadOnly] private SWTCExclusiveCounterA m_ExclusiveCounterA;

            private readonly int m_Value;
            private readonly ProfilerMarker m_ProfilerMarker;

            public SharedReadCounterJob(
                ref SWTCExclusiveCounterA exclusiveCounterA,
                int value,
                ProfilerMarker profilerMarker)
            {
                m_ExclusiveCounterA = exclusiveCounterA;
                m_Value = value;
                m_ProfilerMarker = profilerMarker;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                MathUtil.FindPrimeNumber(SWTCConstants.NTH_PRIME_VALUE_TO_FIND);

                ref readonly int counter = ref m_ExclusiveCounterA.Buffer.ElementAtReadOnly(0);
                Debug.Assert(counter == m_Value);

                m_ProfilerMarker.End();
            }
        }
    }
#endif
}