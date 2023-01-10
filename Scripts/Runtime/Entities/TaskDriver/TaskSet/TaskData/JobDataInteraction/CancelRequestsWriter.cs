using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Job-Safe struct to allow for requesting the cancellation by <see cref="Entity"/>
    /// </summary>
    [BurstCompatible]
    public struct CancelRequestsWriter
    {
        private const int UNSET_LANE_INDEX = -1;
        
        [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_PendingWriter;
        [ReadOnly] private readonly NativeArray<CancelRequestContext> m_CancelRequestContexts;

        private UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter m_PendingLaneWriter;
        private int m_LaneIndex;
        
        internal CancelRequestsWriter(UnsafeTypedStream<EntityProxyInstanceID>.Writer pendingWriter, NativeArray<CancelRequestContext> cancelRequestContexts) : this()
        {
            m_PendingWriter = pendingWriter;
            m_CancelRequestContexts = cancelRequestContexts;

            m_PendingLaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;

            Debug_InitializeWriterState();
        }

        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <remarks>
        /// In most cases this will be called automatically by the Anvil Job type. If using this in a vanilla Unity
        /// Job type, this must be called manually before any other interaction with this struct.
        /// </remarks>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        public void InitForThread(int nativeThreadIndex)
        {
            Debug_EnsureInitThreadOnlyCalledOnce();

            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_PendingLaneWriter = m_PendingWriter.AsLaneWriter(m_LaneIndex);
        }

        /// <summary>
        /// Requests the cancellation of a TaskDriver flow for a specific <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to use for cancellation</param>
        public void RequestCancel(Entity entity)
        {
            RequestCancel(ref entity);
        }

        /// <inheritdoc cref="RequestCancel(Entity)"/>
        public void RequestCancel(ref Entity entity)
        {
            Debug_EnsureCanCancel();
            foreach (CancelRequestContext cancelRequestContext in m_CancelRequestContexts)
            {
                m_PendingLaneWriter.Write(new EntityProxyInstanceID(entity, cancelRequestContext.TaskSetOwnerID, cancelRequestContext.ActiveID));
                UnityEngine.Debug.Log($"Writing Cancel for Entity {entity} in TaskSetOwner {cancelRequestContext.TaskSetOwnerID} to Active {cancelRequestContext.ActiveID}");
            }
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private enum WriterState
        {
            Uninitialized,
            Ready
        }

        private WriterState m_State;
#endif

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_InitializeWriterState()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = WriterState.Uninitialized;
#endif
        }
        

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureCanCancel()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State == WriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to request a cancel");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureInitThreadOnlyCalledOnce()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State != WriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} has already been called!");
            }

            m_State = WriterState.Ready;
#endif
        }
    }
}
