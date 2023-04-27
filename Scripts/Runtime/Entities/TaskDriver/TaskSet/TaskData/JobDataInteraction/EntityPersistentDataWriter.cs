using Anvil.Unity.DOTS.Data;
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

        internal EntityPersistentDataWriter(ref UnsafeParallelHashMap<Entity, TData> lookup)
        {
            m_Lookup = lookup;
        }

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.Count"/>
        public int Count() => m_Lookup.Count();

        /// <summary>
        /// Removes a key-value pair and disposes the value.
        /// </summary>
        /// <param name="entity">The key to remove at.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool RemoveAndDispose(Entity entity)
        {
            if (m_Lookup.Remove(entity, out TData data))
            {
                data.Dispose();
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.Remove"/>
        public bool Remove(Entity entity) => m_Lookup.Remove(entity);

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

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.GetKeyValueArrays"/>>
        public NativeKeyValueArrays<Entity, TData> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            return m_Lookup.GetKeyValueArrays(allocator);
        }

        /// <inheritdoc cref="UnsafeParallelHashMap{TKey,TValue}.GetEnumerator"/>>
        public UnsafeParallelHashMap<Entity, TData>.Enumerator GetEnumerator()
        {
            return m_Lookup.GetEnumerator();
        }
    }
}