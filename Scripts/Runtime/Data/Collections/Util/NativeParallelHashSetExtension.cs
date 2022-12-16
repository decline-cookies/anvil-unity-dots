using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A collection of extension methods for <see cref="NativeParallelHashSet{T}"/>.
    /// </summary>
    public static class NativeParallelHashSetExtension
    {
        /// <summary>
        /// Copy the contents of one <see cref="NativeParallelHashSet{T}"/> to another overwriting any existing data.
        /// </summary>
        /// <param name="destination">The <see cref="NativeParallelHashSet{T}"/> to copy values into.</param>
        /// <param name="source">The <see cref="NativeParallelHashSet{T}"/> to copy values from.</param>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <remarks>
        /// Unlike calling <see cref="NativeParallelHashMap{TKey,TValue}.Clear()"/> and
        /// <see cref="NativeParallelHashMap{TKey,TValue}.UnionWith()"/> this method ensures that the capacity of the
        /// <see cref="destination"/> is pre-allocated.
        /// </remarks>
        [BurstCompile]
        public static void CopyFrom<T>(this NativeParallelHashSet<T> destination, NativeParallelHashSet<T> source)
            where T : unmanaged, IEquatable<T>
        {
            destination.Clear();

            int sourceCount = source.Count();
            if (destination.Capacity < sourceCount)
            {
                destination.Capacity = sourceCount;
            }

            destination.UnionWith(source);
        }


        /// <summary>
        /// Copy the contents of an <see cref="IEnumerable{T}"/> into a <see cref="NativeParallelHashSet{T}"/>
        /// overwriting any existing data.
        /// </summary>
        /// <param name="destination">The <see cref="NativeParallelHashSet{T}"/> to copy values into.</param>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to copy values from.</param>
        /// <typeparam name="T">The element type of the collection</typeparam>
        /// <remarks>
        /// Attempts to pre-allocate the capacity in the destination if the source's count can be determined ahead of
        /// time.
        /// </remarks>
        [BurstCompile]
        public static void CopyFrom<T>(this NativeParallelHashSet<T> destination, IEnumerable<T> source)
            where T : unmanaged, IEquatable<T>
        {
            destination.Clear();

            int? sourceCount = (source as ICollection<T>)?.Count ?? (source as IReadOnlyCollection<T>)?.Count;
            if (sourceCount.HasValue && destination.Capacity < sourceCount)
            {
                destination.Capacity = sourceCount.Value;
            }

            foreach (T val in source)
            {
                destination.Add(val);
            }
        }
    }
}