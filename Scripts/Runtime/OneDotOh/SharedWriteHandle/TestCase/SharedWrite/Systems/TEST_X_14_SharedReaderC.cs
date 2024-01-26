using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_X_13_SharedReaderB))]
    [BurstCompile]
    public partial struct TEST_X_14_SharedReaderC : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_X_14_SharedReaderC)}");
        private SharedReaderSystemPart<SWTCBufferX> m_SharedReaderSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_SharedReaderSystemPart = new SharedReaderSystemPart<SWTCBufferX>(
                                                                               ref state,
                                                                               2,
                                                                               78,
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