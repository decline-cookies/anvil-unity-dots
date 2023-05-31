using Anvil.Unity.DOTS.Util;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
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
        
        public readonly SpawnerID ID;
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
        
        public bool DidPlayback()
        {
            return m_ECB.DidPlayback();
        }

        public Entity SpawnDeferredEntity<TDefinition>(TDefinition definition)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntityArchetype entityArchetype = GetEntityArchetypeForDefinition<TDefinition>();
            Entity entity = m_ECBWriter.CreateEntity(ID.InstanceID, entityArchetype);
            definition.PopulateOnEntity(entity, this);
            return entity;
        }

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

        public void SetComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            m_ECBWriter.SetComponent(ID.InstanceID, e, component);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity e) where T : struct, IBufferElementData
        {
            return m_ECBWriter.SetBuffer<T>(ID.InstanceID, e);
        }

        public void AppendToBuffer<T>(Entity e, T element) where T : struct, IBufferElementData
        {
            m_ECBWriter.AppendToBuffer(ID.InstanceID, e, element);
        }

        public void AddComponent<T>(Entity e) where T : struct, IComponentData
        {
            m_ECBWriter.AddComponent<T>(ID.InstanceID, e);
        }

        public void AddComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            m_ECBWriter.AddComponent(ID.InstanceID, e, component);
        }

        public void AddComponent(Entity e, ComponentTypes componentTypes)
        {
            m_ECBWriter.AddComponent(ID.InstanceID, e, componentTypes);
        }

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
            
            public readonly uint OwningSystemID;
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
