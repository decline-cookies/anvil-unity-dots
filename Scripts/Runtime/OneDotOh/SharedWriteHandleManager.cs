using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Core
{
    [BurstCompile]
    public static class SharedWriteHandleManager
    {
        private struct SharedWriteState
        {
            public JobHandle BeginHandle;
            public JobHandle EndHandle;
            public int ExpectedNumReadHandles;
        }

        [UsedImplicitly]
        // ReSharper disable once ConvertToStaticClass
        private sealed class SharedWriteHandleManagerContext
        {
            private SharedWriteHandleManagerContext() { }
        }

        private static readonly SharedStatic<NativeArray<SharedWriteState>> s_SharedWriteStateByTypeIndex = SharedStatic<NativeArray<SharedWriteState>>.GetOrCreate<SharedWriteHandleManagerContext>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            ref NativeArray<SharedWriteState> data = ref s_SharedWriteStateByTypeIndex.Data;
            if (data.IsCreated)
            {
                data.Dispose();
            }
            data = new NativeArray<SharedWriteState>(TypeManager.MaximumTypesCount, Allocator.Persistent);
        }

        [BurstCompile]
        public static void AcquireSharedWriteHandle<T>(this ref SystemState state, out JobHandle dependsOn, out SharedWriteTrigger sharedWriteTrigger)
            where T : unmanaged, IComponentData
        {
            ComponentType readOnlyType = ComponentType.ReadOnly<T>();
            ComponentType readWriteType = ComponentType.ReadWrite<T>();
            ref NativeArray<SharedWriteState> data = ref s_SharedWriteStateByTypeIndex.Data;
            ref SharedWriteState sharedWriteState = ref data.ElementAt(readWriteType.TypeIndex.Index);


            JobHandle sharedReadHandle = state.EntityManager.GetDependency(readOnlyType);
            JobHandle exclusiveWriteHandle = state.EntityManager.GetDependency(readWriteType);
            EntityManager entityManager = state.EntityManager;
            int numReadHandles = entityManager.GetNumReadHandles(readWriteType);

            if (numReadHandles == 0
             || sharedWriteState.EndHandle != sharedReadHandle
             || sharedWriteState.ExpectedNumReadHandles != numReadHandles)
            {
                sharedWriteState.ExpectedNumReadHandles = numReadHandles;
                dependsOn = exclusiveWriteHandle;
                sharedWriteState.BeginHandle = exclusiveWriteHandle;
                sharedWriteTrigger = SharedWriteTrigger.New;
            }
            else
            {
                dependsOn = sharedWriteState.BeginHandle;
                sharedWriteTrigger = SharedWriteTrigger.Inline;
            }
        }

        [BurstCompile]
        public static void ReleaseSharedWriteHandle<T>(this ref SystemState state, in JobHandle dependsOn)
        {
            // The read handle will automatically be moved forward by the system.
            // We must manually move the write handle forward in case a Read job happens after us.


            ComponentType readWriteType = ComponentType.ReadWrite<T>();

            ref NativeArray<SharedWriteState> data = ref s_SharedWriteStateByTypeIndex.Data;
            ref SharedWriteState sharedWriteState = ref data.ElementAt(readWriteType.TypeIndex.Index);

            JobHandle endHandle = JobHandle.CombineDependencies(sharedWriteState.EndHandle, dependsOn);

            state.EntityManager.AddDependency(endHandle, readWriteType);

            sharedWriteState.EndHandle = endHandle;
            //We would expect one more when our system finishes.
            sharedWriteState.ExpectedNumReadHandles++;
            if (sharedWriteState.ExpectedNumReadHandles == ComponentDependencyManagerInternal.MAX_READ_HANDLES)
            {
                sharedWriteState.ExpectedNumReadHandles = 1;
            }
        }
    }
}