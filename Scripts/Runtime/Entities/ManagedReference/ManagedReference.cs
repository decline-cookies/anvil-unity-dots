using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Holds reference to a managed instance that is registered with <see cref="ManagedReferenceStore"/> in a valid
    /// <see cref="IComponentData"/> structure.
    /// </summary>
    /// <typeparam name="T">The type of the managed instance that is referenced.</typeparam>
    public readonly struct ManagedReference<T> : IComponentData where T : class, IComponentReferencable
    {
        private readonly int ManagedContextHash;

        /// <summary>
        /// Creates a new managed reference from an <see cref="IComponentReferencable"/> instance.
        /// </summary>
        /// <param name="instance">The instance to hold reference too.</param>
        public ManagedReference(T instance)
        {
            ManagedContextHash = ManagedReferenceStore.GetHash(instance);
        }

        /// <summary>
        /// Retrieve the managed instance from the <see cref="ManagedReference{T}"/>.
        /// </summary>
        /// <returns>The managed instance that this structure represents.</returns>
        public T Resolve()
        {
            return ManagedReferenceStore.Get<T>(ManagedContextHash);
        }
    }
}