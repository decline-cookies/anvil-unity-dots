using Anvil.Unity.DOTS.Data;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct EntityProxyActiveConsolidator<TInstance>
        where TInstance : unmanaged, IEntityKeyedTask
    {
        private static readonly int ELEMENT_SIZE = sizeof(EntityKeyedTaskWrapper<TInstance>);


        private UnsafeParallelHashMap<EntityKeyedTaskID, bool> m_CancelRequestsLookup;
        [NativeDisableUnsafePtrRestriction] private readonly void* m_ActiveBufferPointer;
        [NativeDisableUnsafePtrRestriction] private readonly void* m_ActiveCancelBufferPointer;
        private readonly DataTargetID m_ActiveCancelDataTargetID;
        private readonly CancelRequestBehaviour m_CancelRequestBehaviour;


        public EntityProxyActiveConsolidator(ActiveArrayData<EntityKeyedTaskWrapper<TInstance>> activeArrayData) : this()
        {
            m_ActiveBufferPointer = activeArrayData.Active.GetBufferPointer();
            Debug_EnsurePointerNotNull(m_ActiveBufferPointer);

            if (activeArrayData.ActiveCancelData != null)
            {
                ActiveArrayData<EntityKeyedTaskWrapper<TInstance>> activeCancelData = (ActiveArrayData<EntityKeyedTaskWrapper<TInstance>>)activeArrayData.ActiveCancelData;
                m_ActiveCancelBufferPointer = activeCancelData.Active.GetBufferPointer();
                Debug_EnsurePointerNotNull(m_ActiveCancelBufferPointer);

                m_ActiveCancelDataTargetID = activeCancelData.WorldUniqueID;
            }

            m_CancelRequestBehaviour = activeArrayData.CancelRequestBehaviour;
            m_CancelRequestsLookup = ((ITaskSetOwner)activeArrayData.DataOwner).TaskSet.CancelRequestsDataStream.ActiveLookupData.Lookup;
        }

        public void PrepareForConsolidation()
        {
            DeferredNativeArray<EntityKeyedTaskWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityKeyedTaskWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Clear();

            if (m_ActiveCancelBufferPointer != null)
            {
                deferredNativeArray = DeferredNativeArray<EntityKeyedTaskWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveCancelBufferPointer);
                deferredNativeArray.Clear();
            }
        }

        public void WriteToActive(EntityKeyedTaskWrapper<TInstance> instance)
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

        private void WriteToActiveWithDefaultCancel(ref EntityKeyedTaskWrapper<TInstance> instance)
        {
            //If it exists in the lookup, don't write it to the native array, let it poof out of existence
            if (m_CancelRequestsLookup.ContainsKey(instance.InstanceID))
            {
                return;
            }
            //Otherwise it wasn't cancelled so write it
            WriteInstanceToActive(ref instance);
        }

        private void WriteToActiveWithExplicitCancel(ref EntityKeyedTaskWrapper<TInstance> instance)
        {
            if (m_CancelRequestsLookup.ContainsKey(instance.InstanceID))
            {
                DeferredNativeArray<EntityKeyedTaskWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityKeyedTaskWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveCancelBufferPointer);
                deferredNativeArray.Add(new EntityKeyedTaskWrapper<TInstance>(ref instance, m_ActiveCancelDataTargetID));
                return;
            }
            //Otherwise it wasn't cancelled so write it
            WriteInstanceToActive(ref instance);
        }

        private void WriteInstanceToActive(ref EntityKeyedTaskWrapper<TInstance> instance)
        {
            DeferredNativeArray<EntityKeyedTaskWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityKeyedTaskWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveBufferPointer);
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