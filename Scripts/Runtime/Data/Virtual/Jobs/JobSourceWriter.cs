using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public struct JobSourceWriter<T>
        where T : struct
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_SourceWriter;

        private UnsafeTypedStream<T>.LaneWriter m_SourceLaneWriter;
        private int m_LaneIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private bool m_IsInitializedForThread;
#endif


        public JobSourceWriter(UnsafeTypedStream<T>.Writer sourceWriter) : this()
        {
            m_SourceWriter = sourceWriter;

            m_SourceLaneWriter = default;
            m_LaneIndex = DEFAULT_LANE_INDEX;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_IsInitializedForThread = false;
#endif
        }

        public void InitForThread(int nativeThreadIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(!m_IsInitializedForThread);
            m_IsInitializedForThread = true;
#endif

            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_SourceLaneWriter = m_SourceWriter.AsLaneWriter(m_LaneIndex);
        }

        public void Add(T value)
        {
            Add(ref value);
        }

        public void Add(ref T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_IsInitializedForThread);
#endif
            m_SourceLaneWriter.Write(ref value);
        }
    }
}
