using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    public struct VDLookupReader<TKey, TInstance>
        where TKey : unmanaged, IEquatable<TKey>
        where TInstance : unmanaged
    {
        [ReadOnly] private UnsafeParallelHashMap<TKey, TInstance> m_Lookup;

        internal VDLookupReader(UnsafeParallelHashMap<TKey, TInstance> lookup)
        {
            m_Lookup = lookup;
        }

        public bool ContainsKey(TKey key)
        {
            return m_Lookup.ContainsKey(key);
        }

        public TInstance this[TKey key]
        {
            get => m_Lookup[key];
        }

        public int Count()
        {
            return m_Lookup.Count();
        }
    }
}
