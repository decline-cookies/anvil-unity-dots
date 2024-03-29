using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    internal class DataOwnerLookup<TWorldUniqueID, TBaseData> : AbstractAnvilBase
        where TBaseData : IWorldUniqueID<TWorldUniqueID>
    {
        public IDataOwner DataOwner { get; }
        
        private readonly Dictionary<TWorldUniqueID, TBaseData> m_Lookup;
        private readonly bool m_IsDataDisposable;

        public DataOwnerLookup(IDataOwner dataOwner, bool isDataDisposable) 
        {
            DataOwner = dataOwner;
            m_IsDataDisposable = isDataDisposable;
            m_Lookup = new Dictionary<TWorldUniqueID, TBaseData>();
        }

        protected override void DisposeSelf()
        {
            if (m_IsDataDisposable)
            {
                foreach (IDisposable disposableData in m_Lookup.Values)
                {
                    disposableData.Dispose();
                }
            }
            m_Lookup.Clear();
            base.DisposeSelf();
        }

        public void Add(TBaseData data)
        {
            if (!m_Lookup.TryAdd(data.WorldUniqueID, data))
            {
                throw new InvalidOperationException($"Trying to add data of type {data.GetType()} to {DataOwner} but it already exists. Please ensure a unique context identifier.");
            }
        }

        public bool TryAdd(TBaseData data)
        {
            return m_Lookup.TryAdd(data.WorldUniqueID, data);
        }

        public TSpecificData Create<TSpecificData>(WorldDataOwnerLookup<TWorldUniqueID, TBaseData>.CreateInstanceFunction<TSpecificData> createInstanceFunction, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            TSpecificData data = createInstanceFunction(DataOwner, uniqueContextIdentifier);
            Add(data);
            return data;
        }
    }
}
