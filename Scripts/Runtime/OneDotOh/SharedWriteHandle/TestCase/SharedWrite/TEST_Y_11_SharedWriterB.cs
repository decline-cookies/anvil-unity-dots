using Anvil.Unity.DOTS.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_Y_10_SharedReaderA))]
    [BurstCompile]
    public partial struct TEST_Y_11_SharedWriterB : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_Y_11_SharedWriterB)}");
        private SharedWriterSystemPart<SWTCBufferY> m_SharedWriterSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_SharedWriterSystemPart = new SharedWriterSystemPart<SWTCBufferY>(
                                                                               ref state,
                                                                               SharedWriteTrigger.New,
                                                                               1,
                                                                               65,
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