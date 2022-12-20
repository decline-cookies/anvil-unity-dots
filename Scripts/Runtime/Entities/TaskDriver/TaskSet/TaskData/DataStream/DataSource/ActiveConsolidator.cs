using Anvil.Unity.DOTS.Data;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal unsafe struct ActiveConsolidator<TInstance> : IDisposable
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private static readonly int ELEMENT_SIZE = sizeof(EntityProxyInstanceWrapper<TInstance>);
        private static readonly int INITIAL_SIZE = (int)math.ceil(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceWrapper<TInstance>>() / 8.0f);
        
        private readonly void* m_ActiveBufferPointer;
        private UnsafeList<EntityProxyInstanceWrapper<TInstance>> m_ConsolidationBuffer;

        public ActiveConsolidator(void* activeBufferPointer) : this()
        {
            m_ActiveBufferPointer = activeBufferPointer;
            m_ConsolidationBuffer = new UnsafeList<EntityProxyInstanceWrapper<TInstance>>(INITIAL_SIZE, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_ConsolidationBuffer.Dispose();
        }

        public void WritePending(EntityProxyInstanceWrapper<TInstance> instance)
        {
            m_ConsolidationBuffer.Add(instance);
        }

        public void DeferredCreate()
        {
            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> deferredNativeArray = DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>.ReinterpretFromPointer(m_ActiveBufferPointer);
            deferredNativeArray.Clear();
            NativeArray<EntityProxyInstanceWrapper<TInstance>> activeArray = deferredNativeArray.DeferredCreate(m_ConsolidationBuffer.Length);
            UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(activeArray), m_ConsolidationBuffer.Ptr, ELEMENT_SIZE * m_ConsolidationBuffer.Length);
            m_ConsolidationBuffer.Clear();
        }
    }
}
