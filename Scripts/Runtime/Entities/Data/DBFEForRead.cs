using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents a <see cref="BufferLookup{T}"/> that can only be read from.
    /// Each <see cref="DynamicBuffer{T}"/> can be read in a separate thread in parallel.
    /// To be used in jobs that allow for reading a specific instance in the DBFE
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IBufferElementData"/> to update.</typeparam>
    public readonly struct DBFEForRead<T> where T : unmanaged, IBufferElementData
    {
        [ReadOnly] private readonly BufferLookup<T> m_DBFE;

        public DBFEForRead(SystemBase system)
        {
            m_DBFE = system.GetBufferLookup<T>(true);
        }

        /// <summary>
        /// Gets the <see cref="DynamicBuffer{T}"/> that corresponds to the passed <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to lookup the <see cref="DynamicBuffer{T}"/></param>
        public DynamicBuffer<T> this[Entity entity]
        {
            get => m_DBFE[entity];
        }

        /// <inheritdoc cref="BufferLookup{T}.HasComponent"/>
        public bool HasBuffer(Entity entity) => m_DBFE.HasBuffer(entity);

        /// <inheritdoc cref="BufferLookup{T}.TryGetBuffer"/>
        public bool TryGetBuffer(Entity entity, out DynamicBuffer<T> component) => m_DBFE.TryGetBuffer(entity, out component);
    }
}