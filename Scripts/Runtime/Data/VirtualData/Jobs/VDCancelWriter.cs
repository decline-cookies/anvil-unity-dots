using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public struct VDCancelWriter
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<uint>.Writer m_CancelWriter;

        private UnsafeTypedStream<uint>.LaneWriter m_CancelLaneWriter;
        private int m_LaneIndex;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private enum CancelWriterState
        {
            Uninitialized,
            Ready
        }

        private CancelWriterState m_State;
#endif
        
        internal VDCancelWriter(UnsafeTypedStream<uint>.Writer cancelWriter) : this()
        {
            m_CancelWriter = cancelWriter;

            m_CancelLaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = CancelWriterState.Uninitialized;
#endif
        }
        
        public void InitForThread(int nativeThreadIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State != CancelWriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} has already been called!");
            }

            m_State = CancelWriterState.Ready;
#endif

            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_CancelLaneWriter = m_CancelWriter.AsLaneWriter(m_LaneIndex);
        }

        public void RequestCancel(uint key)
        {
            RequestCancel(ref key);
        }

        public void RequestCancel(ref uint key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State == CancelWriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to request a cancel");
            }
#endif
            m_CancelLaneWriter.Write(ref key);
        }
        
    }
}
