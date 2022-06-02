using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    public struct StateJobRemoveWriter<TState>
        where TState : struct
    {
        private const int DEFAULT_THREAD_INDEX = -1;
        
        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;
        private readonly UnsafeTypedStream<TState>.Writer m_Writer;
        private UnsafeTypedStream<TState>.LaneWriter m_LaneWriter;

        internal StateJobRemoveWriter(UnsafeTypedStream<TState>.Writer writer, bool isMainThread = false)
        {
            m_Writer = writer;
            m_NativeThreadIndex = DEFAULT_THREAD_INDEX;
            m_LaneWriter = isMainThread
                ? m_Writer.AsLaneWriter(ParallelAccessUtil.CollectionIndexForMainThread())
                : default;
        }

        public void InitForThread()
        {
            //TODO: Collection checks - Ensure this is called before anything else is called
            if (m_LaneWriter.IsCreated)
            {
                return;
            }

            m_LaneWriter = m_Writer.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex));
        }

        public void Remove(TState value)
        {
            Remove(ref value);
        }
        
        public void Remove(ref TState value)
        {
            m_LaneWriter.Write(ref value);
        }
    }
}
