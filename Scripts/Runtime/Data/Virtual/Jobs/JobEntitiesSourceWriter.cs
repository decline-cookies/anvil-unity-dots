using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    //TODO: Can this be more efficient with caching lane writer?
    //TODO: Should we include TKey info?
    public readonly struct JobEntitiesSourceWriter<TSource>
        where TSource : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<TSource>.Writer m_SourceWriter;

        public JobEntitiesSourceWriter(UnsafeTypedStream<TSource>.Writer sourceWriter) : this()
        {
            m_SourceWriter = sourceWriter;
        }

        public void Add(TSource value, int nativeThreadIndex)
        {
            Add(ref value, nativeThreadIndex);
        }

        public void Add(ref TSource value, int nativeThreadIndex)
        {
            m_SourceWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex)).Write(ref value);
        }
    }
}
