using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    public struct VDLookupReader<TInstance>
        where TInstance : unmanaged
    {
        [ReadOnly] private UnsafeParallelHashMap<VDContextID, TInstance> m_Lookup;

        internal VDLookupReader(UnsafeParallelHashMap<VDContextID, TInstance> lookup)
        {
            m_Lookup = lookup;
        }

        public bool ContainsKey(VDContextID id)
        {
            return m_Lookup.ContainsKey(id);
        }

        public TInstance this[VDContextID id]
        {
            get => m_Lookup[id];
        }

        public int Count()
        {
            return m_Lookup.Count();
        }

        public NativeArray<VDContextID> ToKeyArray(Allocator allocator)
        {
            return m_Lookup.GetKeyArray(allocator);
        }

        public NativeArray<TInstance> ToValueArray(Allocator allocator)
        {
            return m_Lookup.GetValueArray(allocator);
        }

        public UnsafeParallelHashMap<VDContextID, TInstance>.Enumerator GetEnumerator()
        {
            return m_Lookup.GetEnumerator();
        }
    }
}
