using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Helper class used to make static lookups a little nicer to use and not entering
    /// nested Dictionary hell.
    /// </summary>
    /// <typeparam name="TContext">The context for this lookup.</typeparam>
    /// <typeparam name="TKey">The key to look up. A specific of <typeparamref name="TContext"/></typeparam>
    /// <typeparam name="TValue">The value to store.</typeparam>
    internal abstract class AbstractLookup<TContext, TKey, TValue> : AbstractAnvilBase
        where TValue : IDisposable
    {
        private readonly Dictionary<TKey, TValue> m_Lookup = new Dictionary<TKey, TValue>();

        protected TContext Context
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

        internal bool ContainsKey(TKey key)
        {
            return m_Lookup.ContainsKey(key);
        }

        internal bool TryGet(TKey key, out TValue value)
        {
            return m_Lookup.TryGetValue(key, out value);
        }

        protected TValue LookupGetOrCreate(TKey key, Func<TKey, TValue> creationFunction)
        { 
            if (!TryGet(key, out TValue value))
            {
                value = creationFunction(key);
                m_Lookup.Add(key, value);
            }

            return value;
        }

        protected void LookupRemove(TKey key)
        {
            //Early return because we're iterating the lookup in the Dispose
            if (IsDisposing)
            {
                return;
            }
            m_Lookup.Remove(key);
        }
    }
}
