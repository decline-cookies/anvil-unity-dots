using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A collection of extension methods for <see cref="NativeParallelHashMap{TKey,TValue}"/>.
    /// </summary>
    public static class NativeParallelHashMapExtension
    {
        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="map">The <see cref="NativeParallelHashMap{TKey,TValue}"/> to remove from.</param>
        /// <param name="key">The key to remove.</param>
        /// <param name="value">The value at the key that was removed</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns>True if a key-value pair was removed.</returns>
        public static bool Remove<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, TKey key, out TValue value)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return map.TryGetValue(key, out value) && map.Remove(key);
        }
    }
}
