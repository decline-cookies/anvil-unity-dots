using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Anvil.Unity.DOTS.Data
{
    public struct StateJobUpdater<TKey, TState>
        where TKey : struct, IEquatable<TKey>
        where TState : struct, IState<TKey>
    {
        private const int DEFAULT_LANE_INDEX = -1;
        
        private readonly UnsafeTypedStream<TKey>.Writer m_RemovalWriter;
        private readonly UnsafeTypedStream<TKey>.Writer m_ContinueWriter;
        private NativeArray<TState> m_Current;

        private UnsafeTypedStream<TKey>.LaneWriter m_RemovalLaneWriter;
        private UnsafeTypedStream<TKey>.LaneWriter m_ContinueLaneWriter;

        public int Length
        {
            get => m_Current.Length;
        }

        public int LaneIndex
        {
            get;
            private set;
        }

        public StateJobUpdater(UnsafeTypedStream<TKey>.Writer removalWriter,
                               UnsafeTypedStream<TKey>.Writer continueWriter,
                                  NativeArray<TState> current)
        {
            m_RemovalWriter = removalWriter;
            m_ContinueWriter = continueWriter;
            m_Current = current;

            m_RemovalLaneWriter = default;
            m_ContinueLaneWriter = default;
            LaneIndex = DEFAULT_LANE_INDEX;
        }

        public void InitForThread(int nativeThreadIndex)
        {
            //TODO: Collection checks - Ensure this is called before anything else is called
            LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_RemovalLaneWriter = m_RemovalWriter.AsLaneWriter(LaneIndex);
            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(LaneIndex);
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
