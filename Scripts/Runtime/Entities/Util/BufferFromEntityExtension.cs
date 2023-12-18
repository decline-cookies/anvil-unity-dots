using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for working with <see cref="BufferLookup{T}"/>
    /// </summary>
    public static class BufferLookupExtension
    {
        /// <summary>
        /// Builds a container to provide in job access to a <see cref="DynamicBuffer{T}"/> on an entity.
        /// </summary>
        /// <param name="lookup">The <see cref="BufferLookup{T}"/> instance to get the <see cref="T"/> from</param>
        /// <param name="entity">The <see cref="Entity" /> to get the <see cref="T"/> from.</param>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="T"/>.</returns>
        public static BufferFromSingleEntity<T> ForSingleEntity<T>(this BufferLookup<T> lookup, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return new BufferFromSingleEntity<T>(lookup, entity);
        }
    }
}