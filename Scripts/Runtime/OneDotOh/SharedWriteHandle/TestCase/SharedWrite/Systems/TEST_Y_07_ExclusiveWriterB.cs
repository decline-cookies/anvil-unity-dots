using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_Y_06_SharedWriterC))]
    [BurstCompile]
    public partial struct TEST_Y_07_ExclusiveWriterB : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_Y_07_ExclusiveWriterB)}");
        private ExclusiveWriterSystemPart<SWTCBufferY> m_ExclusiveWriterSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ExclusiveWriterSystemPart = new ExclusiveWriterSystemPart<SWTCBufferY>(ref state,
                                                                                     1,
                                                                                     98,
                                                                                     s_ProfilerMarker);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_ExclusiveWriterSystemPart.OnUpdate(ref state);
        }
    }
#endif
}