using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a write only reference to a <see cref="IAbstractDataStream{TInstance}"/>
    /// To be used in jobs that only allows for writing of this data.
    /// </summary>
    /// <typeparam name="TInstance">They type of <see cref="IEntityProxyInstance"/> to write</typeparam>
    [BurstCompatible]
    public struct DataStreamPendingWriter<TInstance> where TInstance : unmanaged, IEntityProxyInstance
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer m_PendingWriter;
        [ReadOnly] private readonly DataOwnerID m_DataOwnerID;
        [ReadOnly] private readonly DataTargetID m_DataTargetID;

        private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.LaneWriter m_PendingLaneWriter;
        private int m_LaneIndex;

        internal DataStreamPendingWriter(
            UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer pendingWriter,
            DataOwnerID dataOwnerID,
            DataTargetID dataTargetID)
            : this()
        {
            m_PendingWriter = pendingWriter;
            m_DataOwnerID = dataOwnerID;
            m_DataTargetID = dataTargetID;

            m_PendingLaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;

            Debug_InitializeWriterState();
        }

        internal unsafe DataStreamPendingWriter(
            void* writerPtr,
            DataOwnerID dataOwnerID,
            DataTargetID dataTargetID,
            int laneIndex)
            : this()
        {
            m_PendingWriter = UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer.ReinterpretFromPointer(writerPtr);
            m_DataOwnerID = dataOwnerID;
            m_DataTargetID = dataTargetID;
            m_LaneIndex = laneIndex;

            Debug_EnsureWriterIsValid();

            m_PendingLaneWriter = m_PendingWriter.AsLaneWriter(m_LaneIndex);

            Debug_InitializeWriterStateFromPointer();
        }

        /// <summary>
        /// Call once to initialize the state of this writer for the thread it is running on.
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
        /// Call once to initialize the state of this writer for main thread usage.
        /// </summary>
        public void InitForMainThread()
        {
            Debug_EnsureInitThreadOnlyCalledOnce();
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForMainThread();
            m_PendingLaneWriter = m_PendingWriter.AsLaneWriter(m_LaneIndex);
        }

        /// <summary>
        /// Adds the instance to the <see cref="IAbstractDataStream{TInstance}"/>'s
        /// underlying pending collection to be added the next time the data is
        /// consolidated.
        /// </summary>
        /// <param name="instance">The <see cref="IEntityProxyInstance"/></param>
        public void Add(TInstance instance)
        {
            Add(ref instance);
        }

        /// <inheritdoc cref="Add(TInstance)"/>
        public void Add(ref TInstance instance)
        {
            Debug_EnsureCanAdd();
            m_PendingLaneWriter.Write(
                new EntityProxyInstanceWrapper<TInstance>(
                    instance.Key,
                    m_DataOwnerID,
                    m_DataTargetID,
                    ref instance));
        }

        /// <summary>
        /// Adds the instance to the <see cref="IAbstractDataStream{TInstance}"/>'s
        /// underlying pending collection to be added the next time the data is consolidated.
        /// </summary>
        /// <param name="instance">The <see cref="IEntityProxyInstance"/></param>
        /// <param name="laneIndex">
        /// The collection index to use based on the thread this writer is being
        /// used on. <see cref="ParallelAccessUtil"/> to get the correct index.
        /// </param>
        public void Add(TInstance instance, int laneIndex)
        {
            Add(ref instance, laneIndex);
        }

        /// <inheritdoc cref="Add(TInstance, int)"/>
        public void Add(ref TInstance instance, int laneIndex)
        {
            m_PendingWriter.AsLaneWriter(laneIndex)
                .Write(
                    new EntityProxyInstanceWrapper<TInstance>(
                        instance.Key,
                        m_DataOwnerID,
                        m_DataTargetID,
                        ref instance));
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
        private void Debug_InitializeWriterStateFromPointer()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = WriterState.Ready;
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureCanAdd()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State == WriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} or {nameof(InitForMainThread)} must be called first before attempting to add an element. Or call {nameof(Add)} with the explicit lane index.");
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWriterIsValid()
        {
            if (!m_PendingWriter.IsCreated)
            {
                throw new InvalidOperationException($"Tried to reinterpret a pointer as the writer but the data at that pointer is not valid!");
            }
        }
    }
}