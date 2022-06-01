namespace Anvil.Unity.DOTS.Entities
{
    public interface ISystemDataJobWriter<T>
        where T : struct
    {
        //TODO: Docs
        void InitForThread();
        void Add(T value);
        void Add(ref T value);
    }
}
