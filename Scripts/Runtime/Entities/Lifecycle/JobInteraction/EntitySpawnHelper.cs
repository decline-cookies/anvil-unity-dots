using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    public readonly struct EntitySpawnHelper
    {
        [ReadOnly] private readonly NativeParallelHashMap<long, EntityArchetype> m_ArchetypeLookup;
        [ReadOnly] private readonly NativeParallelHashMap<long, Entity> m_PrototypeLookup;

        public EntitySpawnHelper(
            NativeParallelHashMap<long, EntityArchetype> archetypeLookup,
            NativeParallelHashMap<long, Entity> prototypeLookup) 
        {
            m_ArchetypeLookup = archetypeLookup;
            m_PrototypeLookup = prototypeLookup;
        }

        public EntityArchetype GetEntityArchetypeForDefinition<TDefinition>()
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            long hash = BurstRuntime.GetHashCode64<TDefinition>();
            return GetEntityArchetypeForDefinition(hash);
        }
        
        public EntityArchetype GetEntityArchetypeForDefinition(long hash)
        {
            Debug_EnsureArchetypeIsRegistered(hash);
            return m_ArchetypeLookup[hash];
        }

        public Entity GetPrototypeEntityForDefinition<TDefinition>()
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            long hash = BurstRuntime.GetHashCode64<TDefinition>();
            return GetPrototypeEntityForDefinition(hash);
        }
        
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
