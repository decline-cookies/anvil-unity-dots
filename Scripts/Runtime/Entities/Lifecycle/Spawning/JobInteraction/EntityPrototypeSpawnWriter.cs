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
    /// passed in <see cref="IEntitySpawnDefinition"/>s. Uses a prototype <see cref="Entity"/> to clone.
    /// Entities will be spawned later on via the <see cref="EntitySpawnSystem"/>
    /// </summary>
    /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/> to spawn.</typeparam>
    [BurstCompatible]
    public struct EntityPrototypeSpawnWriter<TEntitySpawnDefinition>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>>.Writer m_Writer;
        [ReadOnly] private readonly UnsafeTypedStream<Entity>.Writer m_PrototypesToDestroyWriter;

        private UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>>.LaneWriter m_LaneWriter;
        private UnsafeTypedStream<Entity>.LaneWriter m_PrototypesToDestroyLaneWriter;
        private int m_LaneIndex;

        internal EntityPrototypeSpawnWriter(UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>>.Writer writer,
                                            UnsafeTypedStream<Entity>.Writer prototypesToDestroyWriter) : this()
        {
            m_Writer = writer;
            m_PrototypesToDestroyWriter = prototypesToDestroyWriter;

            m_LaneWriter = default;
            m_PrototypesToDestroyLaneWriter = default;
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
            m_PrototypesToDestroyLaneWriter = m_PrototypesToDestroyWriter.AsLaneWriter(m_LaneIndex);
        }

        /// <summary>
        /// Call once to initialize the state of this writer for main thread usage.
        /// </summary>
        public void InitForMainThread()
        {
            Debug_EnsureInitThreadOnlyCalledOnce();
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForMainThread();
            m_LaneWriter = m_Writer.AsLaneWriter(m_LaneIndex);
            m_PrototypesToDestroyLaneWriter = m_PrototypesToDestroyWriter.AsLaneWriter(m_LaneIndex);
        }

        /// <summary>
        /// Adds the <see cref="IEntitySpawnDefinition"/> to the queue to be spawned
        /// later on by the <see cref="EntitySpawnSystem"/>
        /// </summary>
        /// <param name="prototype">The prototype <see cref="Entity"/> to clone.</param>
        /// <param name="definition">The type of <see cref="IEntitySpawnDefinition"/> to spawn</param>
        /// <param name="shouldDestroyPrototype">
        /// If true, will destroy the prototype <see cref="Entity"/> after creation.
        /// </param>
        public void SpawnDeferred(Entity prototype, TEntitySpawnDefinition definition, bool shouldDestroyPrototype)
        {
            SpawnDeferred(prototype, ref definition, shouldDestroyPrototype);
        }

        /// <inheritdoc cref="SpawnDeferred(Entity,TEntitySpawnDefinition,bool)"/>
        public void SpawnDeferred(Entity prototype, ref TEntitySpawnDefinition definition, bool shouldDestroyPrototype)
        {
            Debug_EnsureCanAdd();
            m_LaneWriter.Write(new EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>(prototype, ref definition));
            if (shouldDestroyPrototype)
            {
                m_PrototypesToDestroyLaneWriter.Write(prototype);
            }
        }

        /// <summary>
        /// Adds the <see cref="IEntitySpawnDefinition"/> to the queue to be spawned later on by
        /// the <see cref="EntitySpawnSystem"/>
        /// </summary>
        /// <param name="prototype">The prototype <see cref="Entity"/> to clone.</param>
        /// <param name="definition">The type of <see cref="IEntitySpawnDefinition"/> to spawn</param>
        /// <param name="laneIndex">
        /// The collection index to use based on the thread this writer is being
        /// used on. <see cref="ParallelAccessUtil"/> to get the correct index.
        /// </param>
        /// <param name="shouldDestroyPrototype">
        /// If true, will destroy the prototype <see cref="Entity"/> after creation.
        /// </param>
        public void SpawnDeferred(Entity prototype, TEntitySpawnDefinition definition, int laneIndex, bool shouldDestroyPrototype)
        {
            SpawnDeferred(prototype, ref definition, laneIndex, shouldDestroyPrototype);
        }

        /// <inheritdoc cref="SpawnDeferred(Entity,TEntitySpawnDefinition,int,bool)"/>
        public void SpawnDeferred(Entity prototype, ref TEntitySpawnDefinition definition, int laneIndex, bool shouldDestroyPrototype)
        {
            m_Writer.AsLaneWriter(laneIndex)
                    .Write(new EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>(prototype, ref definition));
            if (shouldDestroyPrototype)
            {
                m_PrototypesToDestroyWriter.AsLaneWriter(laneIndex)
                                           .Write(prototype);
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
