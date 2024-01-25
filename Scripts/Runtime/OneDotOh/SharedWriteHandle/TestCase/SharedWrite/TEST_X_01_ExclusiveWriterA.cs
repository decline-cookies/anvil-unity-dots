using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct TEST_X_01_ExclusiveWriterA : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_X_01_ExclusiveWriterA)}");
        private ExclusiveWriterSystemPart<SWTCBufferX> m_ExclusiveWriterSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ExclusiveWriterSystemPart = new ExclusiveWriterSystemPart<SWTCBufferX>(ref state,
                                                                                     0,
                                                                                     77,
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