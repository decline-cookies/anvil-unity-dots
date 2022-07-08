using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    public struct VDLookupReader<TInstance>
        where TInstance : unmanaged
    {
        [ReadOnly] private UnsafeParallelHashMap<uint, TInstance> m_Lookup;

        internal VDLookupReader(UnsafeParallelHashMap<uint, TInstance> lookup)
        {
            m_Lookup = lookup;
        }

        public bool ContainsKey(uint key)
        {
            return m_Lookup.ContainsKey(key);
        }

        public TInstance this[uint key]
        {
            get => m_Lookup[key];
        }

        public int Count()
        {
            return m_Lookup.Count();
        }
    }
}
