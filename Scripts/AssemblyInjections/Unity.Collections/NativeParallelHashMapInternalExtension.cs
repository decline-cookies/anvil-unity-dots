using System;
using Unity.Collections;

namespace Anvil.Unity.Collections
{
    /// <summary>
    /// A collection of extension methods for <see cref="NativeParallelHashMap{TKey,TValue}"/> that require internal
    /// access to function.
    /// </summary>
    public static class NativeParallelHashMapInternalExtension
    {
        /// <summary>
        /// Get the <see cref="Allocator"/> of a <see cref="NativeParallelHashMap{TKey,TValue}" />.
        /// </summary>
        /// <param name="map">
        /// The <see cref="NativeParallelHashMap{TKey,TValue}" /> to get the <see cref="Allocator"/> of.
        /// </param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns>The <see cref="Allocator"/>.</returns>
        public static Allocator GetAllocator<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return map.m_HashMapData.GetAllocator();
        }

        /// <summary>
        /// Writes the keys of a <see cref="NativeParallelHashMap{TKey,TValue}"/> to an existing
        /// <see cref="NativeArray{T}"/>.
        /// </summary>
        /// <param name="map">The <see cref="NativeParallelHashMap{TKey,TValue}"/> to get the keys of.</param>
        /// <param name="result">The array to write the keys into.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        public static void GetKeyArray<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, NativeArray<TKey> result)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            map.m_HashMapData.GetKeyArray(result);
        }

        /// <summary>
        /// Writes the values of a <see cref="NativeParallelHashMap{TKey,TValue}"/> to an existing
        /// <see cref="NativeArray{T}"/>.
        /// </summary>
        /// <param name="map">The <see cref="NativeParallelHashMap{TKey,TValue}"/> to get the values of.</param>
        /// <param name="result">The array to write the values into.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        public static void GetValueArray<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, NativeArray<TValue> result)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            map.m_HashMapData.GetValueArray(result);
        }

        /// <summary>
        /// Writes the key value pairs of a <see cref="NativeParallelHashMap{TKey,TValue}"/> to an existing
        /// <see cref="NativeKeyValueArrays{TKey, TValue}"/>
        /// </summary>
        /// <param name="map">The <see cref="NativeParallelHashMap{TKey,TValue}"/> to get the key value pairs of.</param>
        /// <param name="result">The array to write the key value pairs into.</param>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TValue">The value type</typeparam>
        public static void GetKeyValueArrays<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, NativeKeyValueArrays<TKey, TValue> result)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            map.m_HashMapData.GetKeyValueArrays(result);
        }
    }
}
