using Anvil.Unity.DOTS.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_Y_08_SharedWriterA))]
    [BurstCompile]
    public partial struct TEST_Y_09_SharedWriterC : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_Y_09_SharedWriterC)}");
        private SharedWriterSystemPart<SWTCBufferY> m_SharedWriterSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_SharedWriterSystemPart = new SharedWriterSystemPart<SWTCBufferY>(
                                                                               ref state,
                                                                               SharedWriteTrigger.Inline,
                                                                               2,
                                                                               43,
                                                                               s_ProfilerMarker);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_SharedWriterSystemPart.OnUpdate(ref state);
        }
    }
#endif
}