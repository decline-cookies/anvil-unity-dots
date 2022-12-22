using Anvil.Unity.DOTS.Data;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal readonly unsafe struct EntityProxyActiveConsolidator<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private static readonly int ELEMENT_SIZE = sizeof(EntityProxyInstanceWrapper<TInstance>);


        [NativeDisableUnsafePtrRestriction] private readonly void* m_ActiveBufferPointer;

        public EntityProxyActiveConsolidator(void* activeBufferPointer) : this()
        {
            Debug_EnsurePointerNotNull(activeBufferPointer);
            m_ActiveBufferPointer = activeBufferPointer;
        }

        public void PrepareForConsolidation()
        {
            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Clear();
        }

        public void WritePending(EntityProxyInstanceWrapper<TInstance> instance)
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
