using Anvil.Unity.DOTS.Data;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal unsafe struct EntityProxyActiveConsolidator<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private static readonly int ELEMENT_SIZE = sizeof(EntityProxyInstanceWrapper<TInstance>);


        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_CancelRequestsLookup;
        [NativeDisableUnsafePtrRestriction] private readonly void* m_ActiveBufferPointer;
        private readonly CancelBehaviour m_CancelBehaviour;
        

        public EntityProxyActiveConsolidator(void* activeBufferPointer, 
                                             ActiveArrayData<EntityProxyInstanceWrapper<TInstance>> activeArrayData) : this()
        {
            Debug_EnsurePointerNotNull(activeBufferPointer);
            m_ActiveBufferPointer = activeBufferPointer;
            m_CancelBehaviour = activeArrayData.CancelBehaviour;
            m_CancelRequestsLookup = activeArrayData.TaskSetOwner.TaskSet.CancelRequestsDataStream.ActiveLookupData.Lookup;
        }

        public void PrepareForConsolidation()
        {
            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Clear();
        }

        public void WriteToActive(EntityProxyInstanceWrapper<TInstance> instance)
        {
            switch (m_CancelBehaviour)
            {
                case CancelBehaviour.Default:
                    WriteToActiveWithDefaultCancel(ref instance);
                    break;
                case CancelBehaviour.Explicit:
                    WriteToActiveWithExplicitCancel(ref instance);
                    break;
                case CancelBehaviour.None:
                    WriteInstanceToActive(ref instance);
                    break;
                default:
                    throw new InvalidOperationException($"No code path satisfies! {m_CancelBehaviour}");
            }
        }

        private void WriteToActiveWithDefaultCancel(ref EntityProxyInstanceWrapper<TInstance> instance)
        {
            UnityEngine.Debug.Log($"Checking for Default Cancel of {instance.InstanceID.Entity} on TaskSetOwner {instance.InstanceID.TaskSetOwnerID}");
            //If it exists in the lookup, don't write it to the native array, let it poof out of existence
            if (m_CancelRequestsLookup.ContainsKey(instance.InstanceID))
            {
                UnityEngine.Debug.Log("FOUND, DELETING");
                return;
            }
            UnityEngine.Debug.Log("Not cancelled, continuing");
            //Otherwise it wasn't cancelled so write it
            WriteInstanceToActive(ref instance);
        }

        private void WriteToActiveWithExplicitCancel(ref EntityProxyInstanceWrapper<TInstance> instance)
        {
            UnityEngine.Debug.Log($"Checking for Explicit Cancel of {instance.InstanceID.Entity} on TaskSetOwner {instance.InstanceID.TaskSetOwnerID}");
            if (m_CancelRequestsLookup.ContainsKey(instance.InstanceID))
            {
                UnityEngine.Debug.Log("FOUND, DELETING");
                //TODO: Write to Pending Cancelled 
                return;
            }
            UnityEngine.Debug.Log("Not cancelled, continuing");
            //Otherwise it wasn't cancelled so write it
            WriteInstanceToActive(ref instance);
        }

        private void WriteInstanceToActive(ref EntityProxyInstanceWrapper<TInstance> instance)
        {
            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Add(instance);
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsurePointerNotNull(void* ptr)
        {
            if (ptr == null)
            {
                throw new InvalidOperationException($"Trying to reinterpret the writer from a pointer but the pointer is null!");
            }
        }
    }
}
