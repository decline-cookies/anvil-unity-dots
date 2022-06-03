using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    public struct StateJobReader<TKey, TState>
        where TKey : struct, IEquatable<TKey>
        where TState : struct, IState<TKey>
    {
        private const int DEFAULT_THREAD_INDEX = -1;

        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

        private readonly UnsafeTypedStream<TKey>.Writer m_RemovalWriter;
        private readonly NativeArray<TState> m_Current;

        private UnsafeTypedStream<TKey>.LaneWriter m_RemovalLaneWriter;
        
        public int Length
        {
            get => m_Current.Length;
        }

        public StateJobReader(UnsafeTypedStream<TKey>.Writer removalWriter,
                              NativeArray<TState> current)
        {
            m_RemovalWriter = removalWriter;
            m_Current = current;

            m_RemovalLaneWriter = default;
            m_NativeThreadIndex = DEFAULT_THREAD_INDEX;
        }

        public void InitForThread(int nativeThreadIndex)
        {
            //TODO: Collection checks - Ensure this is called before anything else is called
            m_RemovalLaneWriter = m_RemovalWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex));
        }

        public TState this[int index]
        {
            get => m_Current[index];
        }

        public void Remove(TKey value)
        {
            //TODO: Collection checks
            m_RemovalLaneWriter.Write(ref value);
        }
    }
}
