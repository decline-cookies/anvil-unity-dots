using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompile]
    public static class SystemStateInternal
    {
        [BurstCompile]
        public static void GetReadersAndWriters(ref this SystemState state, out UnsafeList<TypeIndex> readers, out UnsafeList<TypeIndex> writers)
        {
            readers = state.m_JobDependencyForReadingSystems;
            writers = state.m_JobDependencyForWritingSystems;
        }
    }
}