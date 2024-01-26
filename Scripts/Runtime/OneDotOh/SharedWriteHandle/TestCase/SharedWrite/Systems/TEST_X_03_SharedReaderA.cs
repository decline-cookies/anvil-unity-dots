using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_X_02_ExclusiveWriterB))]
    [BurstCompile]
    public partial struct TEST_X_03_SharedReaderA : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_X_03_SharedReaderA)}");
        private SharedReaderSystemPart<SWTCBufferX> m_SharedReaderSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_SharedReaderSystemPart = new SharedReaderSystemPart<SWTCBufferX>(
                                                                               ref state,
                                                                               0,
                                                                               77,
                                                                               s_ProfilerMarker);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_SharedReaderSystemPart.OnUpdate(ref state);
        }
    }
#endif
}