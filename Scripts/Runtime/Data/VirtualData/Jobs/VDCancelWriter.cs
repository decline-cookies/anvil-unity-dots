using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public struct VDCancelWriter
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<VDContextID>.Writer m_CancelWriter;
        private readonly int m_Context;

        private UnsafeTypedStream<VDContextID>.LaneWriter m_CancelLaneWriter;
        private int m_LaneIndex;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private enum CancelWriterState
        {
            Uninitialized,
            Ready
        }

        private CancelWriterState m_State;
#endif
        
        internal VDCancelWriter(UnsafeTypedStream<VDContextID>.Writer cancelWriter, int context) : this()
        {
            m_CancelWriter = cancelWriter;
            m_Context = context;

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

        public void Cancel(VDContextID id)
        {
            Cancel(ref id);
        }

        public void Cancel(ref VDContextID id)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State == CancelWriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to request a cancel");
            }
#endif
            //Ensure context is updated
            id.Context = m_Context;
            
            m_CancelLaneWriter.Write(ref id);
        }
        
    }
}
