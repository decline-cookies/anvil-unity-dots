using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper utility to get Archetypes and Prototypes based on the type of <see cref="IEntitySpawnDefinition"/>
    /// </summary>
    [BurstCompatible]
    public readonly struct EntitySpawnHelper
    {
        [ReadOnly] private readonly NativeParallelHashMap<long, EntityArchetype> m_ArchetypeLookup;
        [ReadOnly] private readonly NativeParallelHashMap<long, Entity> m_PrototypeLookup;

        internal EntitySpawnHelper(
            NativeParallelHashMap<long, EntityArchetype> archetypeLookup,
            NativeParallelHashMap<long, Entity> prototypeLookup) 
        {
            m_ArchetypeLookup = archetypeLookup;
            m_PrototypeLookup = prototypeLookup;
        }

        /// <summary>
        /// Gets the <see cref="EntityArchetype"/> for a given <see cref="IEntitySpawnDefinition"/>
        /// </summary>
        /// <typeparam name="TDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>The <see cref="EntityArchetype"/></returns>
        public EntityArchetype GetEntityArchetypeForDefinition<TDefinition>()
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
        public EntityArchetype GetEntityArchetypeForDefinition(long hash)
        {
            Debug_EnsureArchetypeIsRegistered(hash);
            return m_ArchetypeLookup[hash];
        }
        
        /// <summary>
        /// Gets the <see cref="Entity"/> prototype for a given <see cref="IEntitySpawnDefinition"/>
        /// </summary>
        /// <typeparam name="TDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>The <see cref="Entity"/> prototype</returns>
        public Entity GetPrototypeEntityForDefinition<TDefinition>()
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
        public Entity GetPrototypeEntityForDefinition(long hash)
        {
            Debug_EnsurePrototypeIsRegistered(hash);
            return m_PrototypeLookup[hash];
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
    }
}
