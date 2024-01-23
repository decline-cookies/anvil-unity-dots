using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
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
        public static void Reset()
        {
            ref NativeArray<SharedWriteState> data = ref s_SharedWriteStateByTypeIndex.Data;
            data.FloodClear();
        }

        [BurstCompile]
        public static void AcquireSharedWriteHandle<T>(this ref SystemState state, out JobHandle dependsOn)
            where T : unmanaged, IComponentData
        {
            ComponentType readOnlyType = ComponentType.ReadOnly<T>();
            ComponentType readWriteType = ComponentType.ReadWrite<T>();
            ref NativeArray<SharedWriteState> data = ref s_SharedWriteStateByTypeIndex.Data;
            ref SharedWriteState sharedWriteState = ref data.ElementAt(readOnlyType.TypeIndex.Index);


            JobHandle sharedReadHandle = state.EntityManager.GetDependency(readOnlyType);
            JobHandle exclusiveWriteHandle = state.EntityManager.GetDependency(readWriteType);

            // bool t1 = sharedReadHandle == sharedWriteState.BeginHandle;
            // bool t2 = sharedReadHandle == sharedWriteState.EndHandle;
            // bool t3 = sharedReadHandle == state.Dependency;
            //
            // Debug.Log($"SharedRead: {t1}, {t2}, {t3}");
            //
            // bool t4 = exclusiveWriteHandle == sharedWriteState.BeginHandle;
            // bool t5 = exclusiveWriteHandle == sharedWriteState.EndHandle;
            // bool t6 = exclusiveWriteHandle == state.Dependency;
            //
            // Debug.Log($"Exclusive Write: {t4}, {t5}, {t6}");
            //
            // bool t7 = sharedReadHandle.DependsOn(sharedWriteState.EndHandle);
            // bool t8 = exclusiveWriteHandle.DependsOn(sharedWriteState.EndHandle);
            // bool t9 = state.Dependency.DependsOn(sharedWriteState.EndHandle);
            //
            // Debug.Log($"On SWE: {t7}, {t8}, {t9}");
            //
            // bool t11 = sharedWriteState.BeginHandle.DependsOn(sharedReadHandle);
            // bool t12 = sharedWriteState.BeginHandle.DependsOn(exclusiveWriteHandle);
            // bool t13 = sharedWriteState.BeginHandle.DependsOn(sharedWriteState.EndHandle);
            // bool t14 = sharedWriteState.BeginHandle.DependsOn(state.Dependency);
            //
            //
            // Debug.Log($"SW - Begin: {t11}, {t12}, {t13}, {t14}");
            //
            // bool t15 = sharedWriteState.EndHandle.DependsOn(sharedReadHandle);
            // bool t16 = sharedWriteState.EndHandle.DependsOn(exclusiveWriteHandle);
            // bool t17 = sharedWriteState.EndHandle.DependsOn(sharedWriteState.BeginHandle);
            // bool t18 = sharedWriteState.EndHandle.DependsOn(state.Dependency);
            //
            // Debug.Log($"SW - End: {t15}, {t16}, {t17}, {t18}");


            if (sharedWriteState.BeginHandle == default)
            {
                Debug.Log($"New! DEFAULT - Going with exclusive write handle");
                dependsOn = exclusiveWriteHandle;
                sharedWriteState.BeginHandle = exclusiveWriteHandle;
            }
            else if (!sharedWriteState.EndHandle.DependsOn(sharedReadHandle) || !exclusiveWriteHandle.DependsOn(sharedWriteState.EndHandle))
            {
                Debug.Log($"New! DEPENDS - Going with exclusive write handle");
                dependsOn = exclusiveWriteHandle;
                sharedWriteState.BeginHandle = exclusiveWriteHandle;
            }
            else
            {
                Debug.Log($"In Line - Going with existing begin handle");
                dependsOn = sharedWriteState.BeginHandle;
            }
        }

        [BurstCompile]
        public static void ReleaseSharedWriteHandle<T>(this ref SystemState state, in JobHandle dependsOn)
        {
            //Move the Read and Write handles forward to the end of the SharedWrite job.
            ComponentType readWriteType = ComponentType.ReadWrite<T>();
            ComponentType readOnlyType = ComponentType.ReadOnly<T>();

            ref NativeArray<SharedWriteState> data = ref s_SharedWriteStateByTypeIndex.Data;
            ref SharedWriteState sharedWriteState = ref data.ElementAt(readOnlyType.TypeIndex.Index);

            JobHandle endHandle = JobHandle.CombineDependencies(sharedWriteState.EndHandle, dependsOn);

            // state.EntityManager.AddDependency(endHandle, readOnlyType);
            state.EntityManager.AddDependency(dependsOn, readWriteType);

            //Marking where the current write handle
            sharedWriteState.EndHandle = endHandle;
        }
    }
}