using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    public struct JobDataForWork<T>
        where T : struct
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_ContinueWriter;
        [ReadOnly] private readonly NativeArray<T> m_Current;

        private UnsafeTypedStream<T>.LaneWriter m_ContinueLaneWriter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private bool m_IsInitializedForThread;
#endif

        public int LaneIndex
        {
            get;
            private set;
        }

        public JobDataForWork(UnsafeTypedStream<T>.Writer continueWriter,
                              NativeArray<T> current)
        {
            m_ContinueWriter = continueWriter;
            m_Current = current;

            m_ContinueLaneWriter = default;
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
            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(LaneIndex);
        }

        public T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Debug.Assert(m_IsInitializedForThread);
#endif

                return m_Current[index];
            }
        }

        internal void Continue(ref T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_IsInitializedForThread);
#endif
            m_ContinueLaneWriter.Write(ref value);
        }
    }
}
