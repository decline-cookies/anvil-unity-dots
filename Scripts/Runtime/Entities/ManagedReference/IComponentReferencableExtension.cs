namespace Anvil.Unity.DOTS.Data
{
    public static class IComponentReferencableExtension
    {
        public static ManagedReference<T> AsComponentDataReference<T>(this T instance) where T : class, IComponentReferencable
        {
            return new ManagedReference<T>(instance);
        }
    }
}