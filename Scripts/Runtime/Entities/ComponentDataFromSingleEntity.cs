using Unity.Entities;


namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A container that provides access to an <see cref="IComponentData" /> from a single entity.
    /// </summary>
    /// <typeparam name="T">The type that implements <see cref="IComponentData" /></typeparam>
    /// <remarks>Allows developers to define jobs with fewer parameters that clearly communicate intent.</remarks>
    public readonly struct ComponentDataFromSingleEntity<T> where T : struct, IComponentData
    {
        private readonly ComponentDataFromEntity<T> m_Lookup;
        private readonly Entity m_Entity;

        /// <summary>
        /// Creates a new <see cref="ComponentDataFromSingleEntity{T}"/>.
        /// </summary>
        /// <param name="lookup">The <see cref="ComponentDataFromEntity{T}" /> lookup to read the component reference from.</param>
        /// <param name="entity">The <see cref="Entity" /> that the <see cref="{T}" /> is on.</param>
        public ComponentDataFromSingleEntity(ComponentDataFromEntity<T> lookup, Entity entity)
        {
            m_Lookup = lookup;
            m_Entity = entity;
        }

        /// <summary>
        /// Gets the <see cref="DynamicBuffer{T}" />. 
        /// Call during job execution.
        /// </summary>
        /// <returns>The <see cref="T" /> instance</returns>
        public T GetComponent()
        {
            return m_Lookup[m_Entity];
        }
    }
}