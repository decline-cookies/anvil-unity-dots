using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        public static unsafe void AcquireSharedWriteHandle<T>(this ref SystemState state, out JobHandle dependsOn, out SharedWriteTrigger sharedWriteTrigger)
            where T : unmanaged, IComponentData
        {
            ComponentType readOnlyType = ComponentType.ReadOnly<T>();
            ComponentType readWriteType = ComponentType.ReadWrite<T>();
            ref NativeArray<SharedWriteState> data = ref s_SharedWriteStateByTypeIndex.Data;
            ref SharedWriteState sharedWriteState = ref data.ElementAt(readWriteType.TypeIndex.Index);

            //For the Shared Write type, get the Read and Write handles to see where those have moved to.
            JobHandle sharedReadHandle = state.EntityManager.GetDependency(readOnlyType);
            JobHandle exclusiveWriteHandle = state.EntityManager.GetDependency(readWriteType);
            EntityManager entityManager = state.EntityManager;
            int numReadHandles = entityManager.GetNumReadHandles(readWriteType);

            //From the system we'll figure out what all the types are and if they are set for readers or writers.
            //Our Shared Write types should be set to Read for this system.
            state.GetReadersAndWriters(
                                       out UnsafeList<TypeIndex> readers,
                                       out UnsafeList<TypeIndex> writers);

            // Remove the shared write type from the readers list.
            UnsafeList<TypeIndex> modifiedReaders = new UnsafeList<TypeIndex>(readers.Length - 1, Allocator.Temp);
            modifiedReaders.AddRange(readers.Ptr, readers.Length);
            int sharedWriteIndex = -1;
            for (int i = 0; i < modifiedReaders.Length; ++i)
            {
                ref readonly TypeIndex typeIndex = ref modifiedReaders.ElementAtReadOnly(i);
                if (typeIndex != readOnlyType.TypeIndex)
                {
                    continue;
                }
                sharedWriteIndex = i;
                break;
            }
            Debug.Assert(sharedWriteIndex != -1);
            modifiedReaders.RemoveAtSwapBack(sharedWriteIndex);

            JobHandle originalSystemHandle = state.Dependency;
            //Get the modified system handle that ignores our shared write type.
            JobHandle systemHandle = entityManager.GetDependency(modifiedReaders.Ptr, modifiedReaders.Length, writers.Ptr, writers.Length);

            // bool e1 = sharedReadHandle == exclusiveWriteHandle;
            // bool e2 = sharedReadHandle == systemHandle;
            // bool e3 = sharedReadHandle == sharedWriteState.BeginHandle;
            // bool e4 = sharedReadHandle == sharedWriteState.EndHandle;
            // bool o1 = sharedReadHandle == originalSystemHandle;
            //
            // bool e5 = exclusiveWriteHandle == systemHandle;
            // bool e6 = exclusiveWriteHandle == sharedWriteState.BeginHandle;
            // bool e7 = exclusiveWriteHandle == sharedWriteState.EndHandle;
            // bool o2 = exclusiveWriteHandle == originalSystemHandle;
            //
            // bool e8 = systemHandle == sharedWriteState.BeginHandle;
            // bool e9 = systemHandle == sharedWriteState.EndHandle;
            // bool o3 = systemHandle == originalSystemHandle;
            //
            // bool e10 = sharedWriteState.BeginHandle == sharedWriteState.EndHandle;
            // bool o4 = sharedWriteState.BeginHandle == originalSystemHandle;
            //
            // bool o5 = sharedWriteState.EndHandle == originalSystemHandle;
            //
            // bool d1 = sharedReadHandle.DependsOn(exclusiveWriteHandle);
            // bool d2 = sharedReadHandle.DependsOn(systemHandle);
            // bool d3 = sharedReadHandle.DependsOn(sharedWriteState.BeginHandle);
            // bool d4 = sharedReadHandle.DependsOn(sharedWriteState.EndHandle);
            // bool o6 = sharedReadHandle.DependsOn(originalSystemHandle);
            //
            // bool d5 = exclusiveWriteHandle.DependsOn(sharedReadHandle);
            // bool d6 = exclusiveWriteHandle.DependsOn(systemHandle);
            // bool d7 = exclusiveWriteHandle.DependsOn(sharedWriteState.BeginHandle);
            // bool d8 = exclusiveWriteHandle.DependsOn(sharedWriteState.EndHandle);
            // bool o7 = exclusiveWriteHandle.DependsOn(originalSystemHandle);
            //
            // bool d9 = systemHandle.DependsOn(sharedReadHandle);
            // bool d10 = systemHandle.DependsOn(exclusiveWriteHandle);
            // bool d11 = systemHandle.DependsOn(sharedWriteState.BeginHandle);
            // bool d12 = systemHandle.DependsOn(sharedWriteState.EndHandle);
            // bool o8 = systemHandle.DependsOn(originalSystemHandle);
            //
            // bool d13 = sharedWriteState.BeginHandle.DependsOn(sharedReadHandle);
            // bool d14 = sharedWriteState.BeginHandle.DependsOn(exclusiveWriteHandle);
            // bool d15 = sharedWriteState.BeginHandle.DependsOn(systemHandle);
            // bool d16 = sharedWriteState.BeginHandle.DependsOn(sharedWriteState.EndHandle);
            // bool o9 = sharedWriteState.BeginHandle.DependsOn(originalSystemHandle);
            //
            // bool d17 = sharedWriteState.EndHandle.DependsOn(sharedReadHandle);
            // bool d18 = sharedWriteState.EndHandle.DependsOn(exclusiveWriteHandle);
            // bool d19 = sharedWriteState.EndHandle.DependsOn(systemHandle);
            // bool d20 = sharedWriteState.EndHandle.DependsOn(sharedWriteState.BeginHandle);
            // bool o10 = sharedWriteState.EndHandle.DependsOn(originalSystemHandle);
            //
            // bool d21 = originalSystemHandle.DependsOn(sharedReadHandle);
            // bool d22 = originalSystemHandle.DependsOn(exclusiveWriteHandle);
            // bool d23 = originalSystemHandle.DependsOn(systemHandle);
            // bool d24 = originalSystemHandle.DependsOn(sharedWriteState.BeginHandle);
            // bool d25 = originalSystemHandle.DependsOn(sharedWriteState.EndHandle);
            //
            // Debug.Log(
            //           $"\nEquality | Shared Read: {e1}, {e2}, {e3}, {e4}, {o1}\n"
            //         + $"Equality | Exclusive Write: {e5}, {e6}, {e7}, {o2}\n"
            //         + $"Equality | System: {e8}, {e9}, {o3}\n"
            //         + $"Equality | Shared Write State: {e10}, {o4}\n"
            //         + $"Equality | OG: {o5}\n"
            //         + $"Depends On | Shared Read: {d1}, {d2}, {d3}, {d4}, {o6}\n"
            //         + $"Depends On | Exclusive Write: {d5}, {d6}, {d7}, {d8}, {o7}\n"
            //         + $"Depends On | System: {d9}, {d10}, {d11}, {d12}, {o8}\n"
            //         + $"Depends On | Shared Write State - Begin: {d13}, {d14}, {d15}, {d16}, {o9}\n"
            //         + $"Depends On | Shared Write State - End: {d17}, {d18}, {d19}, {d20}, {o10}\n"
            //         + $"Depends On | OG: {d21}, {d22}, {d23}, {d24}, {d25}\n"
            //         + $"Read Handles: {numReadHandles} / {sharedWriteState.ExpectedNumReadHandles}\n");


            //If we're new, then we can just go with where we would write to our shared write type.
            if (numReadHandles == 0
             || sharedWriteState.EndHandle != sharedReadHandle
             || sharedWriteState.ExpectedNumReadHandles != numReadHandles
             || !sharedReadHandle.DependsOn(systemHandle))
            {
                sharedWriteState.ExpectedNumReadHandles = numReadHandles;
                //Need to merge in all other system dependencies
                dependsOn = JobHandle.CombineDependencies(exclusiveWriteHandle, systemHandle);
                sharedWriteState.BeginHandle = dependsOn;
                sharedWriteTrigger = SharedWriteTrigger.New;
            }
            //Otherwise we want to go back and use the shared handle.
            else
            {
                dependsOn = sharedWriteState.BeginHandle;
                sharedWriteTrigger = SharedWriteTrigger.Inline;
            }
        }

        [BurstCompile]
        public static void ReleaseSharedWriteHandle<T>(this ref SystemState state, ref JobHandle dependsOn)
        {
            //TODO: Might not actually need to bother with the EndHandle at all since that should be handled by the read and write
            // The read handle will automatically be moved forward by the system.
            // We must manually move the write handle forward in case a Read job happens after us.
            ComponentType readWriteType = ComponentType.ReadWrite<T>();
            ComponentType readOnlyType = ComponentType.ReadOnly<T>();

            ref NativeArray<SharedWriteState> data = ref s_SharedWriteStateByTypeIndex.Data;
            ref SharedWriteState sharedWriteState = ref data.ElementAt(readWriteType.TypeIndex.Index);

            // JobHandle endHandle = JobHandle.CombineDependencies(sharedWriteState.EndHandle, dependsOn);

            //We've started back at the shared write handle, but we need to mix in the original read handle so we match the chain.
            JobHandle sharedReadHandle = state.EntityManager.GetDependency(readOnlyType);

            dependsOn = JobHandle.CombineDependencies(state.Dependency, dependsOn);

            //The read handle will get moved forward by the system, but we need to move our write handle forward manually.
            state.EntityManager.AddDependency(dependsOn, readWriteType);

            sharedWriteState.EndHandle = dependsOn;
            //We would expect one more when our system finishes.
            sharedWriteState.ExpectedNumReadHandles++;
            if (sharedWriteState.ExpectedNumReadHandles == ComponentDependencyManagerInternal.MAX_READ_HANDLES)
            {
                sharedWriteState.ExpectedNumReadHandles = 1;
            }
        }
    }
}