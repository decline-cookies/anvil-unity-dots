using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a read only reference to a <see cref="ComponentDataFromEntity{T}"/>
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IComponentData"/> to read</typeparam>
    [BurstCompatible]
    public readonly struct CDFEReader<T>
        where T : struct, IComponentData
    {
        [ReadOnly] private readonly ComponentDataFromEntity<T> m_CDFE;

        internal CDFEReader(SystemBase system)
        {
            m_CDFE = system.GetComponentDataFromEntity<T>(true);
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> that corresponds to the passed <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to lookup the data</param>
        public T this[Entity entity]
        {
            get => m_CDFE[entity];
        }

        /// <inheritdoc cref="ComponentDataFromEntity{T}.HasComponent"/>
        public bool HasComponent(Entity entity) => m_CDFE.HasComponent(entity);
    }
}