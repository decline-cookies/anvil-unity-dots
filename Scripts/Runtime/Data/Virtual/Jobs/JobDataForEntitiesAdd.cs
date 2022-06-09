using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    //TODO: Can this be more efficient with caching lane writer?
    //TODO: Should we include TKey info?
    public struct JobDataForEntitiesAdd<TValue>
        where TValue : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<TValue>.Writer m_AddWriter;

        public JobDataForEntitiesAdd(UnsafeTypedStream<TValue>.Writer addWriter) : this()
        {
            m_AddWriter = addWriter;
        }

        public void Add(TValue value, int nativeThreadIndex)
        {
            Add(ref value, nativeThreadIndex);
        }

        public void Add(ref TValue value, int nativeThreadIndex)
        {
            m_AddWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex)).Write(ref value);
        }
    }
}
