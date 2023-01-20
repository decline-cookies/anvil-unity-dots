using Anvil.Unity.DOTS.Data;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    internal readonly unsafe struct CancelCompleteActiveConsolidator
    {
        [NativeDisableUnsafePtrRestriction] private readonly void* m_ActiveBufferPointer;

        public CancelCompleteActiveConsolidator(void* activeBufferPointer)
        {
            Debug_EnsurePointerNotNull(activeBufferPointer);
            m_ActiveBufferPointer = activeBufferPointer;
        }

        public void PrepareForConsolidation()
        {
            DeferredNativeArray<EntityProxyInstanceID> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceID>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Clear();
        }

        public void WriteToActive(EntityProxyInstanceID id)
        {
            DeferredNativeArray<EntityProxyInstanceID> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceID>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Add(id);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
        //TODO: #139 - Switch to ANVIL_DEBUG_SAFETY
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
