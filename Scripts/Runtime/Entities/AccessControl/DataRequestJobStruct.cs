
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    public struct DataRequestJobStruct<T>
        where T : struct, ICompleteData<T>
    {
        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;
        private UnsafeTypedStream<T>.Writer m_NewWriter;
        
        private UnsafeTypedStream<T>.LaneWriter m_NewLaneWriter;
        private int m_LaneIndex;

        public DataRequestJobStruct(UnsafeTypedStream<T>.Writer newWriter)
        {
            m_NewWriter = newWriter;

            m_NewLaneWriter = default;
            m_NativeThreadIndex = -1;
            m_LaneIndex = -1;
        }
        
        public void InitForThread()
        {
            //TODO: Collection checks - Ensure this is called before anything else is called
            if (m_NewLaneWriter.IsCreated)
            {
                return;
            }
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
            m_NewLaneWriter = m_NewWriter.AsLaneWriter(m_LaneIndex);
        }

        public void Add(T value)
        {
            Add(ref value);
        }
        
        public void Add(ref T value)
        {
            m_NewLaneWriter.Write(ref value);
        }
    }
}
