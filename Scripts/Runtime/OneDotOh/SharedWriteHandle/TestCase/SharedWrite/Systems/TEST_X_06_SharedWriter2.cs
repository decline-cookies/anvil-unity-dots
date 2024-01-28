using Anvil.Unity.DOTS.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_X_05_SharedWriter1))]
    [BurstCompile]
    public partial struct TEST_X_06_SharedWriter2 : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_X_06_SharedWriter2)}");
        private SharedWriterSystemPart<SWTCBufferX> m_SharedWriterSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_SharedWriterSystemPart = new SharedWriterSystemPart<SWTCBufferX>(
                                                                               ref state,
                                                                               SharedWriteTrigger.Inline,
                                                                               2,
                                                                               11,
                                                                               s_ProfilerMarker);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Debug.Log("TEST_X_06_SharedWriter2");
            m_SharedWriterSystemPart.OnUpdate(ref state);
        }
    }
#endif
}