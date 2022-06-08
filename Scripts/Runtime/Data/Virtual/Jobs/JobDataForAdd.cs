using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    public struct JobDataForAdd<T>
        where T : struct
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_AddWriter;

        private UnsafeTypedStream<T>.LaneWriter m_AddLaneWriter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private bool m_IsInitializedForThread;
#endif

        public int LaneIndex
        {
            get;
            private set;
        }

        public JobDataForAdd(UnsafeTypedStream<T>.Writer addWriter) : this()
        {
            m_AddWriter = addWriter;

            m_AddLaneWriter = default;
            LaneIndex = DEFAULT_LANE_INDEX;

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

            LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_AddLaneWriter = m_AddWriter.AsLaneWriter(LaneIndex);
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
            m_AddLaneWriter.Write(ref value);
        }
    }
}
