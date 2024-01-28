using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_X_06_SharedWriter2))]
    [BurstCompile]
    public partial struct TEST_X_07_ExclusiveWriter1 : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_X_07_ExclusiveWriter1)}");
        private ExclusiveWriterSystemPart<SWTCBufferX> m_ExclusiveWriterSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ExclusiveWriterSystemPart = new ExclusiveWriterSystemPart<SWTCBufferX>(ref state,
                                                                                     1,
                                                                                     67,
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