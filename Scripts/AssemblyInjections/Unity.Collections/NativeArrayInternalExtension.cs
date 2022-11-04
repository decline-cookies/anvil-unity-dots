using Unity.Collections;

namespace Anvil.Unity.Collections
{
    /// <summary>
    /// A collection of extension methods for <see cref="NativeArray{T}"/> that require internal access to function.
    /// </summary>
    public static class NativeArrayInternalExtension
    {
        /// <summary>
        /// Get the <see cref="Allocator"/> of a <see cref="NativeArray{T}" />.
        /// </summary>
        /// <param name="map">The <see cref="NativeArray{T}" /> to get the <see cref="Allocator"/> of.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>The <see cref="Allocator"/>.</returns>
        public static Allocator GetAllocator<T>(this NativeArray<T> array) where T : struct
        {
            return array.m_AllocatorLabel;
        }
    }
}