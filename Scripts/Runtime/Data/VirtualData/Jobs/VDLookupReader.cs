using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    public struct VDLookupReader<TInstance>
        where TInstance : unmanaged
    {
        [ReadOnly] private UnsafeParallelHashMap<VDID, TInstance> m_Lookup;

        internal VDLookupReader(UnsafeParallelHashMap<VDID, TInstance> lookup)
        {
            m_Lookup = lookup;
        }

        public bool ContainsKey(VDID id)
        {
            return m_Lookup.ContainsKey(id);
        }

        public TInstance this[VDID id]
        {
            get => m_Lookup[id];
        }

        public int Count()
        {
            return m_Lookup.Count();
        }
    }
}
