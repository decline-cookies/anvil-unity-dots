using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    public static class UnsafeListExtension
    {
        public static unsafe ref readonly T ElementAtReadOnly<T>(ref this UnsafeList<T> unsafeList, int index) where T : unmanaged
        {
            Debug.Assert(index >= 0 && index < unsafeList.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(unsafeList.Ptr, index);
        }
    }
}