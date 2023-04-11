using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a write only reference to a <see cref="IEntityPersistentData{TData}"/>
    /// To be used in jobs that only allows for writing of this data.
    /// </summary>
    /// <typeparam name="TData">They type of <see cref="IEntityPersistentDataInstance"/> to write</typeparam>
    [BurstCompatible]
    public struct EntityPersistentDataWriter<TData>
        where TData : struct, IEntityPersistentDataInstance
    {
        [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction]
        private UnsafeParallelHashMap<Entity, TData> m_Lookup;

        /// <summary>
        /// Gets or sets the data based on the <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to use as the key</param>
        public TData this[Entity entity]
        {
            get => m_Lookup[entity];
            set => m_Lookup[entity] = value;
        }

        /// <summary>
        /// Returns whether the persistent data has no entries.
        /// </summary>
        public bool IsEmpty
        {
            get => m_Lookup.IsEmpty;
        }

        /// <summary>
        /// Returns the number of entries currently stored in persistent data.
        /// </summary>
        public int Count
        {
            get => m_Lookup.Count();
        }

        internal EntityPersistentDataWriter(ref UnsafeParallelHashMap<Entity, TData> lookup)
        {
            m_Lookup = lookup;
        }

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.Remove"/>
        public bool Remove(Entity entity) => m_Lookup.Remove(entity);

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.TryGetValue"/>
        public bool TryGet(Entity entity, out TData data) => m_Lookup.TryGetValue(entity, out data);

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.ContainsKey"/>
        public bool Contains(Entity entity) => m_Lookup.ContainsKey(entity);
    }
}