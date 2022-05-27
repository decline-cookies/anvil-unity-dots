using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    public struct ResponseJobData<T>
        where T : struct
    {
        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;
        private UnsafeTypedStream<T>.Writer m_CompleteWriter;
        
        private UnsafeTypedStream<T>.LaneWriter m_CompleteLaneWriter;
        private int m_LaneIndex;
        
        public ResponseJobData(UnsafeTypedStream<T>.Writer completeWriter)
        {
            m_CompleteWriter = completeWriter;

            m_CompleteLaneWriter = default;
            m_NativeThreadIndex = -1;
            m_LaneIndex = -1;
        }

        public void InitForThread()
        {
            //TODO: Collection checks - Ensure this is called before anything else is called
            if (m_CompleteLaneWriter.IsCreated)
            {
                return;
            }
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
            m_CompleteLaneWriter = m_CompleteWriter.AsLaneWriter(m_LaneIndex);
        }
        
        public void Add(T value)
        {
            Add(ref value);
        }
        
        public void Add(ref T value)
        {
            m_CompleteLaneWriter.Write(ref value);
        }
    }
}
