using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    public struct PersistentDataReader<TData>
        where TData : struct
    {
        [ReadOnly] public TData Data;
        internal PersistentDataReader(ref TData data)
        {
            Data = data;
        }
    }
}