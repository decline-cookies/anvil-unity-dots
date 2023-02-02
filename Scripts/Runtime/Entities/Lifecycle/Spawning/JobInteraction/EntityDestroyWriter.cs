using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper for queuing up <see cref="Entity"/>s to Destroy in a job.
    /// Entities will be Destroyed later on via the <see cref="EntityDestroySystem"/>
    /// </summary>
    [BurstCompatible]
    public struct EntityDestroyWriter
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<Entity>.Writer m_Writer;

        private UnsafeTypedStream<Entity>.LaneWriter m_LaneWriter;
        private int m_LaneIndex;

        internal EntityDestroyWriter(UnsafeTypedStream<Entity>.Writer writer) : this()
        {
            m_Writer = writer;

            m_LaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;
            
            Debug_InitializeWriterState();
        }

        /// <summary>
        /// Call once to initialize the state of this writer for the thread it is running on.
        /// </summary>
        /// /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        public void InitForThread(int nativeThreadIndex)
        {
            Debug_EnsureInitThreadOnlyCalledOnce();
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_LaneWriter = m_Writer.AsLaneWriter(m_LaneIndex);
        }

        /// <summary>
        /// Call once to initialize the state of this writer for main thread usage.
        /// </summary>
        public void InitForMainThread()
        {
            Debug_EnsureInitThreadOnlyCalledOnce();
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForMainThread();
            m_LaneWriter = m_Writer.AsLaneWriter(m_LaneIndex);
        }
        
        /// <summary>
        /// Adds the <see cref="Entity"/> to the queue to be destroyed
        /// later on by the <see cref="EntityDestroySystem"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to destroy</param>
        public void DestroyDeferred(Entity entity)
        {
            Debug_EnsureCanAdd();
            m_LaneWriter.Write(ref entity);
        }

        /// <summary>
        /// Adds the <see cref="Entity"/> to the queue to be destroyed
        /// later on by the <see cref="EntityDestroySystem"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to destroy</param>
        /// <param name="laneIndex">
        /// The collection index to use based on the thread this writer is being
        /// used on. <see cref="ParallelAccessUtil"/> to get the correct index.
        /// </param>
        public void DestroyDeferred(Entity entity, int laneIndex)
        {
            m_Writer.AsLaneWriter(laneIndex)
                    .Write(ref entity);
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
        private void Debug_EnsureCanAdd()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State == WriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} or {nameof(InitForMainThread)} must be called first before attempting to add an element. Or call {nameof(DestroyDeferred)} with the explicit lane index.");
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
