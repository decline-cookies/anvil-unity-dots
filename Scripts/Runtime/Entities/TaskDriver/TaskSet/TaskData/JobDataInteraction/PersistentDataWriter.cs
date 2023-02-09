using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    public readonly unsafe struct PersistentDataWriter<TData> where TData : struct
    {
        public ref TData Data
        {
            get => ref UnsafeUtility.AsRef<TData>(m_Data);
        }

        private readonly void* m_Data;

        public PersistentDataWriter(ref TData data)
        {
            m_Data = UnsafeUtility.AddressOf(ref data);
        }
    }
}