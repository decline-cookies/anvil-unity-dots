using System.Collections;
using System.Collections.Generic;
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

        internal EntityPersistentDataReader(ref UnsafeParallelHashMap<Entity, TData> lookup)
        {
            m_Lookup = lookup;
        }

        /// <summary>
        /// Gets the <typeparamref name="TData"/> for the specified <see cref="Entity"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to use as the key</param>
        public TData this[Entity entity]
        {
            get => m_Lookup[entity];
        }

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.GetKeyArray"/>>
        public NativeArray<Entity> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            return m_Lookup.GetKeyArray(allocator);
        }

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.GetValueArray"/>>
        public NativeArray<TData> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            return m_Lookup.GetValueArray(allocator);
        }

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.GetEnumerator"/>>
        public UnsafeParallelHashMap<Entity, TData>.Enumerator GetEnumerator()
        {
            return m_Lookup.GetEnumerator();
        }
    }
}