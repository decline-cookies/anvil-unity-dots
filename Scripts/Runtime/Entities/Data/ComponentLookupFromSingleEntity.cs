using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A container that provides access to an <see cref="IComponentData" /> from a single entity.
    /// </summary>
    /// <typeparam name="T">The type that implements <see cref="IComponentData" /></typeparam>
    /// <remarks>Allows developers to define jobs with fewer parameters that clearly communicate intent.</remarks>
    public readonly struct ComponentLookupFromSingleEntity<T> where T : unmanaged, IComponentData
    {
        //TODO: #115 - Implement a safety check to make sure there isn't another lookup/entity combination in flight.
        // Until the above is implemented leave it to consuming jobs to add the attributes.
        // [NativeDisableContainerSafetyRestriction]
        private readonly ComponentLookup<T> m_Lookup;
        private readonly Entity m_Entity;

        /// <summary>
        /// Creates a new <see cref="ComponentLookupFromSingleEntity{T}"/>.
        /// </summary>
        /// <param name="lookup">The <see cref="ComponentLookup{T}" /> lookup to read the component reference from.</param>
        /// <param name="entity">The <see cref="Entity" /> that the <see cref="{T}" /> is on.</param>
        public ComponentLookupFromSingleEntity(ComponentLookup<T> lookup, Entity entity)
        {
            m_Lookup = lookup;
            m_Entity = entity;
        }

        /// <summary>
        /// Gets the <see cref="T" />.
        /// Call during job execution.
        /// </summary>
        /// <returns>The <see cref="T" /> instance</returns>
        public T GetComponent() => m_Lookup[m_Entity];
    }
}