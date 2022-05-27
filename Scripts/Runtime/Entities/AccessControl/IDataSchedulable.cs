using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IDataSchedulable<T>
        where T : struct, ICompleteData<T>
    {
        DeferredNativeArray<T> ArrayForScheduling { get; }
        int BatchSize { get; }
    }
}
