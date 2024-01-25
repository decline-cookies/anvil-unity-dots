using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SWTCInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new SWTCBufferX(new NativeArray<int>(3, Allocator.Persistent)));
            state.EntityManager.CreateSingleton(new SWTCBufferY(new NativeArray<int>(3, Allocator.Persistent)));

            state.Enabled = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonRW(out RefRW<SWTCBufferX> swtcBufferX))
            {
                swtcBufferX.ValueRW.Buffer.Dispose();
            }
            if (SystemAPI.TryGetSingletonRW(out RefRW<SWTCBufferY> swtcBufferY))
            {
                swtcBufferY.ValueRW.Buffer.Dispose();
            }
        }
    }
#endif
}