using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for <see cref="DynamicBuffer{T}"/>
    /// </summary>
    public static class DynamicBufferExtension
    {
        /// <summary>
        /// Return a read only reference to the element at index.
        /// </summary>
        /// <param name="buffer">The buffer to get the reference from.</param>
        /// <param name="index">The zero-based index.</param>
        /// <typeparam name="T">The data type stored in the buffer. Must be a value type.</typeparam>
        /// <returns>A readonly reference to the element.</returns>
        public static unsafe ref readonly T ElementAtReadOnly<T>(this DynamicBuffer<T> buffer, int index) where T : struct
        {
            Debug.Assert(index >= 0 && index < buffer.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(buffer.GetUnsafeReadOnlyPtr(), index);
        }
    }
}