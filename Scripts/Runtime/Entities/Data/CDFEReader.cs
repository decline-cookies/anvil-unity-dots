using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents a read only reference to a <see cref="ComponentDataFromEntity{T}"/>
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IComponentData"/> to read</typeparam>
    [GenerateTestsForBurstCompatibility]
    public readonly struct CDFEReader<T> where T : unmanaged, IComponentData
    {
        [ReadOnly] private readonly ComponentLookup<T> m_CDFE;

        /// <summary>
        /// Gets the <typeparamref name="T"/> that corresponds to the passed <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to lookup the data</param>
        public T this[Entity entity]
        {
            get => m_CDFE[entity];
        }


        public CDFEReader(SystemBase system)
        {
            m_CDFE = system.GetComponentLookup<T>(true);
        }

        internal CDFEReader(ComponentDataFromEntity<T> rawCDFE)
        {
            m_CDFE = rawCDFE;
        }


        /// <inheritdoc cref="ComponentDataFromEntity{T}.HasComponent"/>
        public bool HasComponent(Entity entity) => m_CDFE.HasComponent(entity);

        /// <inheritdoc cref="ComponentDataFromEntity{T}.TryGetComponent"/>
        public bool TryGetComponent(Entity entity, out T component) => m_CDFE.TryGetComponent(entity, out component);
    }
}