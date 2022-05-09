using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Util
{
    internal abstract class AbstractLookup<TContext, TKey, TValue> : AbstractAnvilBase
        where TValue : IDisposable
    {
        private readonly Dictionary<TKey, TValue> m_Lookup = new Dictionary<TKey, TValue>();

        internal TContext Context
        {
            get;
        }
            
        protected AbstractLookup(TContext context)
        {
            Context = context;
        }

        protected override void DisposeSelf()
        {
            foreach (TValue value in m_Lookup.Values)
            {
                value.Dispose();
            }
            m_Lookup.Clear();
            
            base.DisposeSelf();
        }

        protected TValue GetOrCreate(TKey key, Func<TKey, TValue> creationFunction)
        { 
            if (!m_Lookup.TryGetValue(key, out TValue value))
            {
                value = creationFunction(key);
                m_Lookup.Add(key, value);
            }

            return value;
        }
    }
}
