using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public struct VDJobWriter<TInstance>
        where TInstance : struct
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<TInstance>.Writer m_InstanceWriter;

        private UnsafeTypedStream<TInstance>.LaneWriter m_InstanceLaneWriter;
        private int m_LaneIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private bool m_IsInitializedForThread;
#endif


        public VDJobWriter(UnsafeTypedStream<TInstance>.Writer instanceWriter) : this()
        {
            m_InstanceWriter = instanceWriter;

            m_InstanceLaneWriter = default;
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
            m_InstanceLaneWriter = m_InstanceWriter.AsLaneWriter(m_LaneIndex);
        }

        public void Add(TInstance value)
        {
            Add(ref value);
        }

        public void Add(ref TInstance value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_IsInitializedForThread);
#endif
            m_InstanceLaneWriter.Write(ref value);
        }
    }
}
