using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper for queuing up <see cref="Entity"/>s to Spawn in a job based on
    /// passed in <see cref="IEntitySpawnDefinition"/>s.
    /// Entities will be spawned later on via the <see cref="EntitySpawnSystem"/>
    /// </summary>
    /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/> to spawn.</typeparam>
    [BurstCompatible]
    public struct EntitySpawnWriter<TEntitySpawnDefinition>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<TEntitySpawnDefinition>.Writer m_Writer;

        private UnsafeTypedStream<TEntitySpawnDefinition>.LaneWriter m_LaneWriter;
        private int m_LaneIndex;

        internal EntitySpawnWriter(UnsafeTypedStream<TEntitySpawnDefinition>.Writer writer) : this()
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
        /// Adds the <see cref="IEntitySpawnDefinition"/> to the queue to be spawned
        /// later on by the <see cref="EntitySpawnSystem"/>
        /// </summary>
        /// <param name="definition">The type of <see cref="IEntitySpawnDefinition"/> to spawn</param>
        public void SpawnDeferred(TEntitySpawnDefinition definition)
        {
            SpawnDeferred(ref definition);
        }

        /// <inheritdoc cref="SpawnDeferred"/>
        public void SpawnDeferred(ref TEntitySpawnDefinition definition)
        {
            Debug_EnsureCanAdd();
            m_LaneWriter.Write(ref definition);
        }

        /// <summary>
        /// Adds the <see cref="IEntitySpawnDefinition"/> to the queue to be spawned later on by
        /// the <see cref="EntitySpawnSystem"/>
        /// </summary>
        /// <remarks>
        /// Useful when in a job that only operates on each index or Entity like Entities.ForEach.
        /// There is no opportunity to call <see cref="InitForThread"/> at the start and you can't call
        /// it multiple times. 
        /// </remarks>
        /// <param name="definition">The type of <see cref="IEntitySpawnDefinition"/> to spawn</param>
        /// <param name="laneIndex">
        /// The collection index to use based on the thread this writer is being
        /// used on. <see cref="ParallelAccessUtil"/> to get the correct index.
        /// </param>
        public void SpawnDeferred(TEntitySpawnDefinition definition, int laneIndex)
        {
            SpawnDeferred(ref definition, laneIndex);
        }

        /// <inheritdoc cref="SpawnDeferred(TEntitySpawnDefinition,int)"/>
        public void SpawnDeferred(ref TEntitySpawnDefinition definition, int laneIndex)
        {
            m_Writer.AsLaneWriter(laneIndex)
                .Write(ref definition);
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
                throw new InvalidOperationException($"{nameof(InitForThread)} or {nameof(InitForMainThread)} must be called first before attempting to add an element. Or call {nameof(SpawnDeferred)} with the explicit lane index.");
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
