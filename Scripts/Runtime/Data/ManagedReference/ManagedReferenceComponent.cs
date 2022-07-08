using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    public readonly struct ManagedReferenceComponent<T> : IComponentData where T : class, IComponentReferencable
    {
        private readonly int ManagedContextHash;

        public ManagedReferenceComponent(T instance)
        {
            ManagedContextHash = ManagedReferenceStore.GetHash(instance);
        }

        public T Resolve()
        {
            return ManagedReferenceStore.Get<T>(ManagedContextHash);
        }
    }
}