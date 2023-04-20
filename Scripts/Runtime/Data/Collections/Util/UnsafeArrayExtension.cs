using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Extension methods for use with a <see cref="UnsafeArray{T}"/> instance.
    /// </summary>
    public static class UnsafeArrayExtension
    {
        /// <summary>
        /// Returns a reference to the element at a given index.
        /// </summary>
        /// <param name="unsafeArray">The <see cref="UnsafeArray{T}"/></param>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        public static unsafe ref T ElementAt<T>(this UnsafeArray<T> unsafeArray, int index) where T : unmanaged
        {
            Debug.Assert(index >= 0 && index < unsafeArray.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(unsafeArray.GetUnsafePtr(), index);
        }
        
        /// <summary>
        /// Returns a read only reference to the element at a given index.
        /// </summary>
        /// <param name="unsafeArray">The <see cref="UnsafeArray{T}"/></param>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        public static ref readonly T ElementAtReadOnly<T>(this UnsafeArray<T> unsafeArray, int index) where T : unmanaged
        {
            return ref unsafeArray.ElementAt(index);
        }
    }
}
