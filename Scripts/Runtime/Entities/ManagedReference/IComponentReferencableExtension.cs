namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A collection of methods to use with <see cref="IComponentReferencable"/> instances.
    /// </summary>
    public static class IComponentReferencableExtension
    {
        /// <summary>
        /// Creates a <see cref="ManagedReference{T}"/> from an <see cref="IComponentReferencable"/> instance.
        /// Used to reference a managed instance in ECS.
        /// </summary>
        /// <param name="instance">The instance to reference.</param>
        /// <typeparam name="T">The type of the instance.</typeparam>
        /// <returns>
        /// A <see cref="ManagedReference{T}"/> that references the <see cref="instance"/> provided.
        /// </returns>
        public static ManagedReference<T> AsComponentDataReference<T>(this T instance) where T : class, IComponentReferencable
        {
            return new ManagedReference<T>(instance);
        }
    }
}