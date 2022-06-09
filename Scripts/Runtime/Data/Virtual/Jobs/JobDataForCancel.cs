using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    public struct JobDataForCancel<T>
        where T : struct
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_CancelWriter;

        private UnsafeTypedStream<T>.LaneWriter m_CancelLaneWriter;
        private int m_LaneIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private bool m_IsInitializedForThread;
#endif

        
        
        public JobDataForCancel(UnsafeTypedStream<T>.Writer cancelWriter) : this()
        {
            m_CancelWriter = cancelWriter;

            m_CancelLaneWriter = default;
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
            m_CancelLaneWriter = m_CancelWriter.AsLaneWriter(m_LaneIndex);
        }

        public void RequestCancel(T value)
        {
            RequestCancel(ref value);
        }

        public void RequestCancel(ref T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_IsInitializedForThread);
#endif
            m_CancelLaneWriter.Write(ref value);
        }
    }
}
