using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Extension methods for use with a <see cref="NativeArray{T}"/> instance.
    /// </summary>
    public static class NativeArrayExtension
    {
        /// <summary>
        /// Returns a reference to the element at a given index.
        /// </summary>
        /// <param name="nativeArray">The <see cref="NativeArray{T}"/></param>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        public static unsafe ref T ElementAt<T>(this NativeArray<T> nativeArray, int index) where T : struct
        {
            Debug.Assert(index >= 0 && index < nativeArray.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(nativeArray.GetUnsafePtr(), index);
        }

        /// <summary>
        /// Returns a read only reference to the element at a given index.
        /// </summary>
        /// <param name="nativeArray">The <see cref="NativeArray{T}"/></param>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        public static unsafe ref readonly T ElementAtReadOnly<T>(this NativeArray<T> nativeArray, int index) where T : struct
        {
            Debug.Assert(index >= 0 && index < nativeArray.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(nativeArray.GetUnsafeReadOnlyPtr(), index);
        }

        /// <summary>
        /// Sets all elements in a collection to their default value.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="array">The collection to flood clear.</param>
        public static unsafe void FloodClear<T>(this NativeArray<T> array) where T : struct
            => array.FloodClear(0, array.Length);

        /// <summary>
        /// Sets a range of elements in a collection to their default value.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="array">The collection to flood clear.</param>
        /// <param name="startIndex">The index to start at.</param>
        /// <param name="length">The number of elements to clear.</param>
        public static unsafe void FloodClear<T>(this NativeArray<T> array, int startIndex, int length) where T : struct
        {
            Debug.Assert(startIndex < array.Length);
            Debug.Assert(startIndex + length <= array.Length);

            UnsafeCollectionUtil.FloodClearBuffer<T>(array.GetUnsafePtr(), startIndex, length);
        }

        /// <summary>
        /// Sets all elements in a collection to a given value.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="array">The collection to flood set.</param>
        /// <param name="value">The value to set each element to.</param>
        public static unsafe void FloodSet<T>(this NativeArray<T> array, T value) where T : struct
            => array.FloodSet(0, array.Length, value);

        /// <summary>
        /// Sets a range of elements in a collection to a given value.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="array">The collection to flood set.</param>
        /// <param name="startIndex">The index to start at.</param>
        /// <param name="length">The number of elements to set.</param>
        /// <param name="value">The value to set each element to.</param>
        public static unsafe void FloodSet<T>(this NativeArray<T> array, int startIndex, int length, T value) where T : struct
        {
            Debug.Assert(startIndex < array.Length);
            Debug.Assert(startIndex + length <= array.Length);

            UnsafeCollectionUtil.FloodSetBuffer(array.GetUnsafePtr(), startIndex, length, value);
        }
    }
}