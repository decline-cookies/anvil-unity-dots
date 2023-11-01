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
    [GenerateTestsForBurstCompatibility]
    public readonly struct EntityPersistentDataReader<TData>
        where TData : unmanaged, IEntityPersistentDataInstance
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

        internal EntityPersistentDataReader(ref UnsafeParallelHashMap<Entity, TData> lookup)
        {
            m_Lookup = lookup;
        }

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.Count"/>
        public int Count() => m_Lookup.Count();

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.TryGetValue"/>
        public bool TryGet(Entity entity, out TData data) => m_Lookup.TryGetValue(entity, out data);

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.ContainsKey"/>
        public bool Contains(Entity entity) => m_Lookup.ContainsKey(entity);

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