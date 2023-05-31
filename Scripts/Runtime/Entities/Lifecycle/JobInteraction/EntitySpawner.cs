using Anvil.Unity.DOTS.Util;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper struct to allow for spawning <see cref="Entity"/> and making associated structural and initialization
    /// changes while in parallel bursted jobs.
    /// </summary>
    [BurstCompatible]
    public readonly struct EntitySpawner : IEquatable<EntitySpawner>
    {
        public static bool operator ==(EntitySpawner lhs, EntitySpawner rhs)
        {
            return lhs.ID == rhs.ID;
        }

        public static bool operator !=(EntitySpawner lhs, EntitySpawner rhs)
        {
            return !(lhs == rhs);
        }
        
        private const int UNSET_THREAD_INDEX = -1;
        
        /// <summary>
        /// The <see cref="SpawnerID"/> to identify this instance globally in the application
        /// </summary>
        public readonly SpawnerID ID;
        
        /// <summary>
        /// The Thread Index this instance is operating on.
        /// </summary>
        [NativeSetThreadIndex] public readonly int ThreadIndex;
        
        
        [NativeDisableContainerSafetyRestriction] private readonly EntityCommandBuffer m_ECB;
        [NativeDisableContainerSafetyRestriction] private readonly EntityCommandBuffer.ParallelWriter m_ECBWriter;
        [ReadOnly] private readonly NativeParallelHashMap<long, EntityArchetype> m_ArchetypeLookup;
        [ReadOnly] private readonly NativeParallelHashMap<long, Entity> m_PrototypeLookup;
        
        
        internal EntitySpawner(
            SpawnerID id,
            EntityCommandBuffer ecb,
            NativeParallelHashMap<long, EntityArchetype> archetypeLookup,
            NativeParallelHashMap<long, Entity> prototypeLookup)
        {
            ID = id;
            m_ECB = ecb;
            m_ECBWriter = m_ECB.AsParallelWriter();
            m_ArchetypeLookup = archetypeLookup;
            m_PrototypeLookup = prototypeLookup;
            ThreadIndex = UNSET_THREAD_INDEX;
        }
        
        /// <summary>
        /// Returns whether the underlying <see cref="EntityCommandBuffer"/> has been played back yet or not.
        /// </summary>
        /// <returns>true if it has, false if not.</returns>
        public bool DidPlayback()
        {
            return m_ECB.DidPlayback();
        }

        /// <summary>
        /// Spawns an <see cref="Entity"/> based on the passed in <see cref="IEntitySpawnDefinition"/> and uses an
        /// archetype.
        /// This will be actually spawned with the corresponding <see cref="EntityCommandBufferSystem"/> executes.
        /// </summary>
        /// <param name="definition">The <see cref="IEntitySpawnDefinition"/> to use</param>
        /// <typeparam name="TDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>
        /// A deferred <see cref="Entity"/>.
        /// This Entity is invalid but will be patched when the corresponding <see cref="EntityCommandBufferSystem"/>
        /// executes. Any references to this entity must be used/stored via commands run on this instance.
        /// </returns>
        public Entity SpawnDeferredEntity<TDefinition>(TDefinition definition)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntityArchetype entityArchetype = GetEntityArchetypeForDefinition<TDefinition>();
            Entity entity = m_ECBWriter.CreateEntity(ID.InstanceID, entityArchetype);
            definition.PopulateOnEntity(entity, this);
            return entity;
        }
        
        /// <summary>
        /// Spawns an <see cref="Entity"/> based on the passed in <see cref="IEntitySpawnDefinition"/> and uses
        /// a prototype which was registered with <see cref="EntitySpawnSystem.RegisterEntityPrototypeForDefinition"/>.
        /// This will be actually spawned with the corresponding <see cref="EntityCommandBufferSystem"/> executes.
        /// </summary>
        /// <param name="definition">The <see cref="IEntitySpawnDefinition"/> to use</param>
        /// <typeparam name="TDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>
        /// A deferred <see cref="Entity"/>.
        /// This Entity is invalid but will be patched when the corresponding <see cref="EntityCommandBufferSystem"/>
        /// executes. Any references to this entity must be used/stored via commands run on this instance.
        /// </returns>
        public Entity SpawnDeferredEntityWithPrototype<TDefinition>(TDefinition definition)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            Entity prototype = GetPrototypeEntityForDefinition<TDefinition>();
            Entity entity = m_ECBWriter.Instantiate(ID.InstanceID, prototype);
            definition.PopulateOnEntity(entity, this);
            return entity;
        }

        //*************************************************************************************************************
        // SPECIAL COMMAND BUFFER API REPLICATION
        //*************************************************************************************************************

        //TODO: #86 - Remove once we upgrade to Entities 1.0
        /// <summary>
        /// A special managed function that is called by a function pointer to allow for use in Burst. 
        /// </summary>
        /// <param name="spawnerID">The <see cref="SpawnerID"/> to identify the spawner</param>
        /// <param name="threadIndex">The thread index to use</param>
        /// <param name="e">The <see cref="Entity"/> to set the component on</param>
        /// <param name="component">The <see cref="ISharedComponentData"/></param>
        /// <typeparam name="T">The type of <see cref="ISharedComponentData"/></typeparam>
        public static void ManagedLookupAndSetSharedComponent<T>(SpawnerID spawnerID, int threadIndex, Entity e, T component) where T : struct, ISharedComponentData
        {
            EntitySpawner entitySpawner = EntitySpawnSystem.GetEntitySpawnerByID(spawnerID);
            EntityCommandBuffer.ParallelWriter ecbParallelWriter = entitySpawner.m_ECBWriter;
            ecbParallelWriter.SetThreadIndex(threadIndex);
            ecbParallelWriter.SetSharedComponent(entitySpawner.ID.InstanceID, e, component);
        }

        //*************************************************************************************************************
        // ENTITY COMMAND BUFFER API REPLICATION
        //*************************************************************************************************************

        /// <inheritdoc cref="EntityCommandBuffer.SetComponent{T}"/>
        public void SetComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            m_ECBWriter.SetComponent(ID.InstanceID, e, component);
        }

        /// <inheritdoc cref="EntityCommandBuffer.SetBuffer{T}"/>
        public DynamicBuffer<T> SetBuffer<T>(Entity e) where T : struct, IBufferElementData
        {
            return m_ECBWriter.SetBuffer<T>(ID.InstanceID, e);
        }

        /// <inheritdoc cref="EntityCommandBuffer.AppendToBuffer{T}"/>
        public void AppendToBuffer<T>(Entity e, T element) where T : struct, IBufferElementData
        {
            m_ECBWriter.AppendToBuffer(ID.InstanceID, e, element);
        }

        /// <inheritdoc cref="EntityCommandBuffer.AddComponent{T}(Entity)"/>
        public void AddComponent<T>(Entity e) where T : struct, IComponentData
        {
            m_ECBWriter.AddComponent<T>(ID.InstanceID, e);
        }

        /// <inheritdoc cref="EntityCommandBuffer.AddComponent{T}(Entity, T)"/>
        public void AddComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            m_ECBWriter.AddComponent(ID.InstanceID, e, component);
        }

        /// <inheritdoc cref="EntityCommandBuffer.AddComponent(Entity, ComponentTypes)"/>
        public void AddComponent(Entity e, ComponentTypes componentTypes)
        {
            m_ECBWriter.AddComponent(ID.InstanceID, e, componentTypes);
        }

        /// <inheritdoc cref="EntityCommandBuffer.DestroyEntity(Entity)"/>
        public void DestroyEntity(Entity e)
        {
            m_ECBWriter.DestroyEntity(ID.InstanceID, e);
        }

        //*************************************************************************************************************
        // ENTITY COMMAND BUFFER API FOR IMMEDIATE SPAWNING
        //*************************************************************************************************************

        internal void Playback(EntityManager entityManager)
        {
            m_ECB.Playback(entityManager);
        }

        internal void DisposeECB()
        {
            m_ECB.Dispose();
        }

        //*************************************************************************************************************
        // HELPERS
        //*************************************************************************************************************

        /// <summary>
        /// Gets the <see cref="EntityArchetype"/> for a given <see cref="IEntitySpawnDefinition"/>
        /// </summary>
        /// <typeparam name="TDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>The <see cref="EntityArchetype"/></returns>
        internal EntityArchetype GetEntityArchetypeForDefinition<TDefinition>()
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            long hash = BurstRuntime.GetHashCode64<TDefinition>();
            return GetEntityArchetypeForDefinition(hash);
        }

        /// <summary>
        /// Gets the <see cref="EntityArchetype"/> for a given <see cref="IEntitySpawnDefinition"/>
        /// based on the hash derived from <see cref="BurstRuntime.GetHashCode64"/>
        /// </summary>
        /// <param name="hash">The hash to lookup</param>
        /// <returns>The <see cref="EntityArchetype"/></returns>
        internal EntityArchetype GetEntityArchetypeForDefinition(long hash)
        {
            Debug_EnsureArchetypeIsRegistered(hash);
            return m_ArchetypeLookup[hash];
        }

        /// <summary>
        /// Gets the <see cref="Entity"/> prototype for a given <see cref="IEntitySpawnDefinition"/>
        /// </summary>
        /// <typeparam name="TDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>The <see cref="Entity"/> prototype</returns>
        internal Entity GetPrototypeEntityForDefinition<TDefinition>()
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            long hash = BurstRuntime.GetHashCode64<TDefinition>();
            return GetPrototypeEntityForDefinition(hash);
        }

        /// <summary>
        /// Gets the <see cref="Entity"/> prototype for a given <see cref="IEntitySpawnDefinition"/>
        /// based on the hash derived from <see cref="BurstRuntime.GetHashCode64"/>
        /// </summary>
        /// <param name="hash">The hash to lookup</param>
        /// <returns>The <see cref="Entity"/> prototype</returns>
        internal Entity GetPrototypeEntityForDefinition(long hash)
        {
            Debug_EnsurePrototypeIsRegistered(hash);
            return m_PrototypeLookup[hash];
        }
        
        //*************************************************************************************************************
        // IEQUATABLE
        //*************************************************************************************************************
        
        public bool Equals(EntitySpawner other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is EntitySpawner spawner && Equals(spawner);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureArchetypeIsRegistered(long hash)
        {
            if (!m_ArchetypeLookup.ContainsKey(hash))
            {
                throw new InvalidOperationException($"Tried to get the EntityArchetype but it wasn't in the lookup! Something went wrong generating that lookup.");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsurePrototypeIsRegistered(long hash)
        {
            if (!m_PrototypeLookup.ContainsKey(hash))
            {
                throw new InvalidOperationException($"Tried to get the Prototype Entity but it wasn't in the lookup! Did you call {nameof(EntitySpawnSystem.UnregisterEntityPrototypeForDefinition)}?");
            }
        }
        
        //*************************************************************************************************************
        // ID
        //*************************************************************************************************************

        /// <summary>
        /// ID Struct to identifying a <see cref="EntitySpawner"/> globally in the application.
        /// </summary>
        public readonly struct SpawnerID : IEquatable<SpawnerID>
        {
            public static bool operator ==(SpawnerID lhs, SpawnerID rhs)
            {
                return lhs.OwningSystemID == rhs.OwningSystemID && lhs.InstanceID == rhs.InstanceID;
            }

            public static bool operator !=(SpawnerID lhs, SpawnerID rhs)
            {
                return !(lhs == rhs);
            }
            
            /// <summary>
            /// The ID of the <see cref="EntitySpawnSystem"/> that owns this <see cref="EntitySpawner"/>
            /// </summary>
            public readonly uint OwningSystemID;
            
            /// <summary>
            /// The ID of the <see cref="EntitySpawner"/>
            /// </summary>
            public readonly int InstanceID;
            
            public SpawnerID(uint owningSystemID, int instanceID)
            {
                OwningSystemID = owningSystemID;
                InstanceID = instanceID;
            }

            public bool Equals(SpawnerID other)
            {
                return this == other;
            }

            public override bool Equals(object compare)
            {
                return compare is SpawnerID id && Equals(id);
            }

            public override int GetHashCode()
            {
                return HashCodeUtil.GetHashCode((int)OwningSystemID, InstanceID);
            }

            public override string ToString()
            {
                return $"OwningSystemID: {OwningSystemID}, InstanceID: {InstanceID}";
            }
        }
    }
}
