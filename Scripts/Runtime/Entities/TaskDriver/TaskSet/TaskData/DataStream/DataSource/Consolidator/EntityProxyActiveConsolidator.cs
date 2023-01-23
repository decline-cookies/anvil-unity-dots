using Anvil.Unity.DOTS.Data;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    internal unsafe struct EntityProxyActiveConsolidator<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private static readonly int ELEMENT_SIZE = sizeof(EntityProxyInstanceWrapper<TInstance>);


        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_CancelRequestsLookup;
        [NativeDisableUnsafePtrRestriction] private readonly void* m_ActiveBufferPointer;
        [NativeDisableUnsafePtrRestriction] private readonly void* m_PendingCancelActiveBufferPointer;
        private readonly uint m_PendingCancelActiveID;
        private readonly CancelRequestBehaviour m_CancelRequestBehaviour;


        public EntityProxyActiveConsolidator(ActiveArrayData<EntityProxyInstanceWrapper<TInstance>> activeArrayData) : this()
        {
            m_ActiveBufferPointer = activeArrayData.Active.GetBufferPointer();
            Debug_EnsurePointerNotNull(m_ActiveBufferPointer);

            if (activeArrayData.PendingCancelActiveData != null)
            {
                ActiveArrayData<EntityProxyInstanceWrapper<TInstance>> pendingCancelActiveData = (ActiveArrayData<EntityProxyInstanceWrapper<TInstance>>)activeArrayData.PendingCancelActiveData;
                m_PendingCancelActiveBufferPointer = pendingCancelActiveData.Active.GetBufferPointer();
                Debug_EnsurePointerNotNull(m_PendingCancelActiveBufferPointer);

                m_PendingCancelActiveID = pendingCancelActiveData.ID;
            }
            
            m_CancelRequestBehaviour = activeArrayData.CancelRequestBehaviour;
            m_CancelRequestsLookup = activeArrayData.TaskSetOwner.TaskSet.CancelRequestsDataStream.ActiveLookupData.Lookup;
        }

        public void PrepareForConsolidation()
        {
            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Clear();

            if (m_PendingCancelActiveBufferPointer != null)
            {
                deferredNativeArray = DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>.ReinterpretFromPointer(m_PendingCancelActiveBufferPointer);
                deferredNativeArray.Clear();
            }
        }

        public void WriteToActive(EntityProxyInstanceWrapper<TInstance> instance)
        {
            switch (m_CancelRequestBehaviour)
            {
                case CancelRequestBehaviour.Delete:
                    WriteToActiveWithDefaultCancel(ref instance);
                    break;
                case CancelRequestBehaviour.Unwind:
                    WriteToActiveWithExplicitCancel(ref instance);
                    break;
                case CancelRequestBehaviour.Ignore:
                    WriteInstanceToActive(ref instance);
                    break;
                default:
                    throw new InvalidOperationException($"No code path satisfies! {m_CancelRequestBehaviour}");
            }
        }

        private void WriteToActiveWithDefaultCancel(ref EntityProxyInstanceWrapper<TInstance> instance)
        {
            //If it exists in the lookup, don't write it to the native array, let it poof out of existence
            if (m_CancelRequestsLookup.ContainsKey(instance.InstanceID))
            {
                return;
            }
            //Otherwise it wasn't cancelled so write it
            WriteInstanceToActive(ref instance);
        }

        private void WriteToActiveWithExplicitCancel(ref EntityProxyInstanceWrapper<TInstance> instance)
        {
            if (m_CancelRequestsLookup.ContainsKey(instance.InstanceID))
            {
                DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>.ReinterpretFromPointer(m_PendingCancelActiveBufferPointer);
                deferredNativeArray.Add(new EntityProxyInstanceWrapper<TInstance>(ref instance, m_PendingCancelActiveID));
                return;
            }
            //Otherwise it wasn't cancelled so write it
            WriteInstanceToActive(ref instance);
        }

        private void WriteInstanceToActive(ref EntityProxyInstanceWrapper<TInstance> instance)
        {
            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Add(instance);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
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
