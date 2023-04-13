using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a read only reference to a <see cref="IEntityPersistentData{TData}"/>
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    /// <typeparam name="TData">They type of <see cref="IEntityPersistentDataInstance"/> to read</typeparam>
    [BurstCompatible]
    public readonly struct EntityPersistentDataReader<TData>
        where TData : struct, IEntityPersistentDataInstance
    {
        [ReadOnly] private readonly UnsafeParallelHashMap<Entity, TData> m_Lookup;

        /// <summary>
        /// Gets the <typeparamref name="TData"/> for the specified <see cref="Entity"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to use as the key</param>
        public TData this[Entity entity]
        {
            get => m_Lookup[entity];
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

        internal EntityPersistentDataReader(ref UnsafeParallelHashMap<Entity, TData> lookup)
        {
            m_Lookup = lookup;
        }
    }
}