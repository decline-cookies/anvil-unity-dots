namespace Anvil.Unity.DOTS.Data
{
    public static class AbstractComponentReferencableExtension
    {
        public static ManagedReferenceComponent<T> AsComponentDataReference<T>(this T instance) where T : class, IComponentReferencable
        {
            return new ManagedReferenceComponent<T>(instance);
        }
    }
}