using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract unsafe class AbstractTypedPersistentData<T> : AbstractPersistentData
        where T : unmanaged
    {
        private readonly void* m_Data;

        public ref T Data
        {
            get => ref UnsafeUtility.AsRef<T>(m_Data);
        }

        protected AbstractTypedPersistentData(IDataOwner dataOwner, T data, string uniqueContextIdentifier) : base(dataOwner, uniqueContextIdentifier)
        {
            m_Data = UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>(),
                UnsafeUtility.AlignOf<T>(),
                Allocator.Persistent);
            *(T*)m_Data = data;
        }

        protected override void DisposeData()
        {
            (Data as IDisposable)?.Dispose();
            UnsafeUtility.Free(m_Data, Allocator.Persistent);
        }
    }
}
