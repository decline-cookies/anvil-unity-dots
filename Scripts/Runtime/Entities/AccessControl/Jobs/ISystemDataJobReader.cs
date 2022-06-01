namespace Anvil.Unity.DOTS.Entities
{
    public interface ISystemDataJobReader<T>
        where T : struct
    {
        //TODO: Docs
        void InitForThread();
        T this[int index] { get; }
        void Continue(ref T value);
        int Length { get; }
    }
}
