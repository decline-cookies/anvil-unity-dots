using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a write only reference to <see cref="ProxyDataStream{TInstance}"/>
    /// for writing new <typeparamref name="TInstance"/> to.
    /// </summary>
    /// <remarks>
    /// Commonly used to add new instances.
    /// </remarks>
    /// <typeparam name="TInstance">The type of instance to add</typeparam>
    [BurstCompatible]
    public struct DataStreamWriter<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.Writer m_InstanceWriter;
        [ReadOnly] private readonly byte m_Context;

        private UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.LaneWriter m_InstanceLaneWriter;
        private int m_LaneIndex;

        internal DataStreamWriter(UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.Writer instanceWriter, byte context) : this()
        {
            m_InstanceWriter = instanceWriter;
            m_Context = context;

            m_InstanceLaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;

            Debug_InitializeWriterState();
        }

        internal unsafe DataStreamWriter(void* writerPtr,
                                         byte context,
                                         int laneIndex) : this()
        {
            m_InstanceWriter = UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.Writer.ReinterpretFromPointer(writerPtr);
            m_Context = context;
            m_LaneIndex = laneIndex;

            Debug_EnsureWriterIsValid();

            m_InstanceLaneWriter = m_InstanceWriter.AsLaneWriter(m_LaneIndex);

            Debug_InitializeWriterStateFromPointer();
        }

        /// <summary>
        /// Initializes based on the thread it's being used on.
        /// This must be called before doing anything else with the struct.
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index</param>
        public void InitForThread(int nativeThreadIndex)
        {
            Debug_EnsureInitThreadOnlyCalledOnce();

            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_InstanceLaneWriter = m_InstanceWriter.AsLaneWriter(m_LaneIndex);
        }

        /// <summary>
        /// Adds the instance to the <see cref="ProxyDataStream{TInstance}"/>'s
        /// underlying pending collection to be added the next time the virtual data is
        /// consolidated.
        /// </summary>
        /// <param name="instance">The instance to add</param>
        public void Add(TInstance instance)
        {
            Add(ref instance);
        }

        /// <inheritdoc cref="Add(TInstance)"/>
        public void Add(ref TInstance instance)
        {
            Debug_EnsureCanAdd();
            m_InstanceLaneWriter.Write(new ProxyInstanceWrapper<TInstance>(instance.Entity,
                                                                           m_Context,
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
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to add an element.");
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
            if (!m_InstanceWriter.IsCreated)
            {
                throw new InvalidOperationException($"Tried to reinterpret a pointer as the writer but the data at that pointer is not valid!");
            }
        }
    }
}
