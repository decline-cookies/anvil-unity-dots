using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    public struct StateJobLookupReader<TKey, TState>
        where TKey : struct, IEquatable<TKey>
        where TState : struct, IState<TKey>
    {
        [ReadOnly] private readonly NativeHashMap<TKey, TState> m_Lookup;

        public StateJobLookupReader(NativeHashMap<TKey, TState> lookup)
        {
            m_Lookup = lookup;
        }

        public TState this[TKey key]
        {
            get => m_Lookup[key];
        }
        
    }
}
