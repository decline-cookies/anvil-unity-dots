namespace Anvil.Unity.DOTS.Entities
{
    public interface IPersistentData<T> : IAbstractPersistentData
        where T : struct
    {
        public ref T Data { get; }
    }
}
