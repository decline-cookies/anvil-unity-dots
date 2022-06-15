using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    //TODO: Can this be more efficient with caching lane writer?
    //TODO: Should we include TKey info?
    public readonly struct JobInstanceWriterEntities<TInstance>
        where TInstance : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<TInstance>.Writer m_InstanceWriter;

        public JobInstanceWriterEntities(UnsafeTypedStream<TInstance>.Writer instanceWriter) : this()
        {
            m_InstanceWriter = instanceWriter;
        }

        public void Add(TInstance value, int nativeThreadIndex)
        {
            Add(ref value, nativeThreadIndex);
        }

        public void Add(ref TInstance value, int nativeThreadIndex)
        {
            m_InstanceWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex)).Write(ref value);
        }
    }
}
