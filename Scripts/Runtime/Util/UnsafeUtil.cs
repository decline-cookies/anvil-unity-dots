using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// A collection of utilities that help with unsafe work.
    /// </summary>
    public unsafe class UnsafeUtil
    {
        /// <summary>
        /// Determines whether two <see cref="{T}"/> instances are equal without boxing.
        /// </summary>
        /// <param name="value1">The first <see cref="{T}"/> compare.</param>
        /// <param name="value2">The second <see cref="{T}"/> compare.</param>
        /// <returns>True if the <see cref="{T}"/>s are the same.</returns>
        /// <remarks>
        /// Useful for situations where <see cref="System.IEquatable{T}"/> can't be implemented
        /// (Ex: 3rd-party assemblies)
        /// </remarks>
        public static bool Equals_NoBox<T>(in T value1, in T value2)
            where T : unmanaged
        {
            return UnsafeUtility.MemCmp(
                    UnsafeUtilityExtensions.AddressOf(in value1),
                    UnsafeUtilityExtensions.AddressOf(in value2),
                    UnsafeUtility.SizeOf<T>())
                == 0;
        }
    }
}