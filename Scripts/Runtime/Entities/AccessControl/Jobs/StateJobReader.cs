using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    public struct StateJobReader<TState> : ISystemDataJobReader<TState>
        where TState : struct
    {
        private const int DEFAULT_THREAD_INDEX = -1;

        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

        private readonly UnsafeTypedStream<TState>.Writer m_ContinueWriter;
        private readonly NativeArray<TState> m_Current;

        private UnsafeTypedStream<TState>.LaneWriter m_ContinueLaneWriter;
        
        public int Length
        {
            get => m_Current.Length;
        }

        public StateJobReader(UnsafeTypedStream<TState>.Writer continueWriter,
                              NativeArray<TState> current)
        {
            m_ContinueWriter = continueWriter;
            m_Current = current;

            m_ContinueLaneWriter = default;
            m_NativeThreadIndex = DEFAULT_THREAD_INDEX;
        }

        public void InitForThread()
        {
            //TODO: Collection checks - Ensure this is called before anything else is called
            if (m_ContinueLaneWriter.IsCreated)
            {
                return;
            }

            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex));
        }

        public TState this[int index]
        {
            get => m_Current[index];
        }

        public void Continue(ref TState value)
        {
            //TODO: Collection checks
            m_ContinueLaneWriter.Write(ref value);
        }
    }
}
