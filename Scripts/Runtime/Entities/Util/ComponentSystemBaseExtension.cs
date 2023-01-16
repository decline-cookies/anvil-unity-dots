using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for working with <see cref="ComponentSystemBaseExtension"/>
    /// </summary>
    public static class ComponentSystemBaseExtension
    {
        /// <summary>
        /// Builds a container to provide in job access to a singleton <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="isReadOnly">true if the data will not be written to.</param>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="DynamicBuffer{T}"/>.</returns>
        public static BufferFromSingleEntity<T> GetBufferFromSingletonEntity<T>(this ComponentSystemBase system, bool isReadOnly = false) where T : struct, IBufferElementData
        {
            return system.GetBufferFromEntity<T>(isReadOnly).ForSingleEntity(system.GetSingletonEntity<T>());
        }

        /// <summary>
        /// Builds a container to provide in job access to a <see cref="DynamicBuffer{T}"/> on an entity.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to get the <see cref="DynamicBuffer{T}"/> from.</param>
        /// <param name="isReadOnly">true if the data will not be written to.</param>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="DynamicBuffer{T}"/>.</returns>
        /// <remarks>
        /// If fetching many single entity containers at once, getting a <see cref="BufferFromEntity{T}"/> instance and
        /// calling <see cref="BufferFromEntityExtension.ForSingleEntity{T}"/> is more efficient.
        /// </remarks>
        public static BufferFromSingleEntity<T> GetBufferFromSingleEntity<T>(this ComponentSystemBase system, Entity entity, bool isReadOnly = false) where T : struct, IBufferElementData
        {
            return system.GetBufferFromEntity<T>(isReadOnly).ForSingleEntity(entity);
        }

        /// <summary>
        /// Builds a container to provide in job access to a singleton <see cref="IComponentData"/>.
        /// </summary>
        /// <param name="isReadOnly">true if the data will not be written to.</param>
        /// <typeparam name="T">The type that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="T"/>.</returns>
        public static ComponentDataFromSingleEntity<T> GetComponentDataFromSingletonEntity<T>(this ComponentSystemBase system, bool isReadOnly) where T : struct, IComponentData
        {
            return system.GetComponentDataFromEntity<T>(isReadOnly).ForSingleEntity(system.GetSingletonEntity<T>());
        }

        /// <summary>
        /// Builds a container to provide in job access to a <see cref="IComponentData{T}"/> on an entity.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to get the <see cref="T"/> from.</param>
        /// <param name="isReadOnly">true if the data will not be written to.</param>
        /// <typeparam name="T">The element type of the <see cref="IComponentData"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="T"/>.</returns>
        /// <remarks>
        /// If fetching many single entity containers at once, getting a <see cref="ComponentDataFromEntity{T}"/> instance and
        /// calling <see cref="ComponentDataFromEntityExtension.ForSingleEntity{T}"/> is more efficient.
        /// </remarks>
        public static ComponentDataFromSingleEntity<T> GetComponentDataFromSingleEntity<T>(this ComponentSystemBase system, Entity entity, bool isReadOnly) where T : struct, IComponentData
        {
            return system.GetComponentDataFromEntity<T>(isReadOnly).ForSingleEntity(entity);
        }
    }
}