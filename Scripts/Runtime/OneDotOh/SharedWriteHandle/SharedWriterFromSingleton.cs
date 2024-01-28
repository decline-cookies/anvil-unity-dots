using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Core
{
    [BurstCompile]
    public struct SharedWriterFromSingleton<T>
        where T : unmanaged, IComponentData
    {
        private EntityQuery m_SingletonQuery;
        [NativeDisableContainerSafetyRestriction] private T m_Data;

        public SharedWriterFromSingleton(ref SystemState state)
        {
            EntityQueryBuilder queryBuilder = new EntityQueryBuilder(AllocatorManager.Temp)
                                             .WithAll<T>()
                                             .WithOptions(EntityQueryOptions.Default | EntityQueryOptions.IncludeSystems);
            m_SingletonQuery = queryBuilder.Build(ref state);
            queryBuilder.Dispose();

            //Require this for Updating the system
            state.RequireForUpdate<T>();

            m_Data = default;
        }

        public JobHandle Acquire(ref SystemState state)
        {
            state.AcquireSharedWriteHandle<T>(out JobHandle sharedWriteHandle, out _);
            m_Data = m_SingletonQuery.GetSingleton<T>();
            return sharedWriteHandle;
        }

        public void Release(ref SystemState state, in JobHandle dependsOn)
        {
            // state.ReleaseSharedWriteHandle<T>(dependsOn);
        }

        public T Data
        {
            get => m_Data;
        }

    }
}