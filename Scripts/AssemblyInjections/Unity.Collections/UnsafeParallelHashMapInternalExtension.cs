using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.Collections
{
    /// <summary>
    /// A collection of extension methods for <see cref="UnsafeParallelHashMap{TKey,TValue}"/> that require internal
    /// access to function.
    /// </summary>
    public static class UnsafeParallelHashMapInternalExtension
    {
        /// <summary>
        /// Get the <see cref="Allocator"/> of a <see cref="UnsafeParallelHashMap{TKey,TValue}" />.
        /// </summary>
        /// <param name="map">
        /// The <see cref="UnsafeParallelHashMap{TKey,TValue}" /> to get the <see cref="Allocator"/> of.
        /// </param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns>The <see cref="Allocator"/>.</returns>
        public static Allocator GetAllocator<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue> map)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return map.m_AllocatorLabel.ToAllocator;
        }

        /// <summary>
        /// Writes the keys of a <see cref="UnsafeParallelHashMap{TKey,TValue}"/> to an existing
        /// <see cref="NativeArray{T}"/>.
        /// </summary>
        /// <param name="map">The <see cref="UnsafeParallelHashMap{TKey,TValue}"/> to get the keys of.</param>
        /// <param name="result">The array to write the keys into.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        public static unsafe void GetKeyArray<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue> map, NativeArray<TKey> result)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            UnsafeParallelHashMapData.GetKeyArray(map.m_Buffer, result);
        }

        /// <summary>
        /// Writes the values of a <see cref="UnsafeParallelHashMap{TKey,TValue}"/> to an existing
        /// <see cref="NativeArray{T}"/>.
        /// </summary>
        /// <param name="map">The <see cref="UnsafeParallelHashMap{TKey,TValue}"/> to get the values of.</param>
        /// <param name="result">The array to write the values into.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        public static unsafe void GetValueArray<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue> map, NativeArray<TValue> result)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            UnsafeParallelHashMapData.GetValueArray(map.m_Buffer, result);
        }

        /// <summary>
        /// Writes the key value pairs of a <see cref="UnsafeParallelHashMap{TKey,TValue}"/> to an existing
        /// <see cref="NativeKeyValueArrays{TKey, TValue}"/>
        /// </summary>
        /// <param name="map">The <see cref="UnsafeParallelHashMap{TKey,TValue}"/> to get the key value pairs of.</param>
        /// <param name="result">The array to write the key value pairs into.</param>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TValue">The value type</typeparam>
        public static unsafe void GetKeyValueArrays<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue> map, NativeKeyValueArrays<TKey, TValue> result)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            UnsafeParallelHashMapData.GetKeyValueArrays(map.m_Buffer, result);
        }
    }
}
