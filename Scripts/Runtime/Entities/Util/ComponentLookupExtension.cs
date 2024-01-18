using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for working with <see cref="ComponentLookup{T}"/>
    /// </summary>
    public static class ComponentLookupExtension
    {
        /// <summary>
        /// Builds a container to provide in job access to a <see cref="IComponentData{T}"/> on an entity.
        /// </summary>
        /// <param name="lookup">The <see cref="ComponentLookup{T}"/> instance to get the <see cref="T"/> from</param>
        /// <param name="entity">The <see cref="Entity" /> to get the <see cref="T"/> from.</param>
        /// <typeparam name="T">The element type of the <see cref="IComponentData"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="T"/>.</returns>
        public static ComponentLookupFromSingleEntity<T> ForSingleEntity<T>(this ComponentLookup<T> lookup, Entity entity)
            where T : unmanaged, IComponentData
        {
            return new ComponentLookupFromSingleEntity<T>(lookup, entity);
        }
    }
}