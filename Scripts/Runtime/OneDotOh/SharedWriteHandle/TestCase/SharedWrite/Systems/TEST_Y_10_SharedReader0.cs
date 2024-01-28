using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TEST_Y_09_SharedWriter2))]
    [BurstCompile]
    public partial struct TEST_Y_10_SharedReader0 : ISystem
    {
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker($"{nameof(TEST_Y_10_SharedReader0)}");
        private SharedReaderSystemPart<SWTCBufferY> m_SharedReaderSystemPart;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_SharedReaderSystemPart = new SharedReaderSystemPart<SWTCBufferY>(
                                                                               ref state,
                                                                               0,
                                                                               34,
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