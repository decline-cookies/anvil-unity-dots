using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Extension methods for use with a <see cref="NativeArray{T}"/> instance.
    /// </summary>
    public static class NativeArrayExtension
    {
        /// <summary>
        /// Sets all elements in a collection to their default value.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="array">The collection to flood clear.</param>
        public static unsafe void FloodClear<T>(this NativeArray<T> array) where T : struct => array.FloodClear(0, array.Length);
        /// <summary>
        /// Sets a range of elements in a collection to their default value.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="array">The collection to flood clear.</param>
        /// <param name="startIndex">The index to start at.</param>
        /// <param name="length">The number of elements to clear.</param>
        public static unsafe void FloodClear<T>(this NativeArray<T> array, int startIndex, int length) where T : struct
        {
            Debug.Assert(startIndex >= 0);
            Debug.Assert(startIndex < array.Length);
            Debug.Assert(startIndex + length <= array.Length);

            int valSize = UnsafeUtility.SizeOf<T>();
            void* startPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(array) + (valSize * startIndex);
            int clearSize = valSize * length;
            UnsafeUtility.MemClear(startPtr, clearSize);
        }

        /// <summary>
        /// Sets all elements in a collection to a given value.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="array">The collection to flood set.</param>
        /// <param name="value">The value to set each element to.</param>
        public static unsafe void FloodSet<T>(this NativeArray<T> array, T value) where T : struct => array.FloodSet(0, array.Length, value);
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
            Debug.Assert(startIndex >= 0);
            Debug.Assert(startIndex < array.Length);
            Debug.Assert(startIndex + length <= array.Length);

            int valSize = UnsafeUtility.SizeOf<T>();
            void* startPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(array) + (valSize * startIndex);
            void* valPtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpyReplicate(startPtr, valPtr, valSize, length);
        }
    }
}

