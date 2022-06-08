using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    //TODO: Can this be more efficient with caching lane writer?
    public struct JobDataForEntitiesAdd<T>
        where T : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_AddWriter;

        public JobDataForEntitiesAdd(UnsafeTypedStream<T>.Writer addWriter) : this()
        {
            m_AddWriter = addWriter;
        }

        public void Add(T value, int nativeThreadIndex)
        {
            Add(ref value, nativeThreadIndex);
        }

        public void Add(ref T value, int nativeThreadIndex)
        {
            m_AddWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex)).Write(ref value);
        }
    }
}
