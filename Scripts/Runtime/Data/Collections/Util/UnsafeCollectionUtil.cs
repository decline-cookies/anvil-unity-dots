using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A collection of methods for dealing with unsafe collections and pointers to their data.
    /// </summary>
    public static class UnsafeCollectionUtil
    {
        /// <summary>
        /// Sets elements in a collection to their default value.
        /// </summary>
        /// <param name="bufferPtr">A read/write pointer to the start of the collection.</param>
        /// <param name="startIndex">The element index to start clearing at.</param>
        /// <param name="length">
        /// The number of elements to clear.
        /// Warning: This method cannot enforce that this parameter is within the bounds of the collection.
        /// If the value is too large it will write over some other object's data.
        /// </param>
        /// <typeparam name="T">The type of element in the collection.</typeparam>
        public static unsafe void FloodClearBuffer<T>(void* bufferPtr, int startIndex, int length) where T : struct
        {
            Debug.Assert(startIndex >= 0);
            Debug.Assert(length > 0);

            int valueSize = UnsafeUtility.SizeOf<T>();
            bufferPtr = (byte*)bufferPtr + (valueSize * startIndex);
            int clearSize = valueSize * length;
            UnsafeUtility.MemClear(bufferPtr, clearSize);
        }

        /// <summary>
        /// Sets elements in a collection to a given value.
        /// </summary>
        /// <param name="bufferPtr">A read/write pointer to the start of the collection.</param>
        /// <param name="startIndex">The element index to start setting at.</param>
        /// <param name="length">
        /// The number of elements to clear.
        /// Warning: This method cannot enforce that this parameter is within the bounds of the collection.
        /// If the value is too large it will write over some other object's data.
        /// </param>
        /// <param name="value">The value to set each element.</param>
        /// <typeparam name="T">The type of element in the collection.</typeparam>
        public static unsafe void FloodSetBuffer<T>(void* bufferPtr, int startIndex, int length, T value)
            where T : struct
        {
            Debug.Assert(startIndex >= 0);
            Debug.Assert(length > 0);

            int valueSize = UnsafeUtility.SizeOf<T>();
            void* valuePtr = UnsafeUtility.AddressOf(ref value);
            bufferPtr = (byte*)bufferPtr + (valueSize * startIndex);
            UnsafeUtility.MemCpyReplicate(bufferPtr, valuePtr, valueSize, length);
        }
    }
}