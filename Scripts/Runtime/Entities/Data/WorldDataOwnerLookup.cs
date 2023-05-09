using Anvil.CSharp.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    internal class WorldDataOwnerLookup<TWorldUniqueID, TBaseData> : AbstractAnvilBase,
                                                                     IEnumerable<KeyValuePair<TWorldUniqueID, TBaseData>> 
        where TBaseData : IWorldUniqueID<TWorldUniqueID>
    {
        private static readonly Type I_DISPOSABLE = typeof(IDisposable);
        
        public delegate TSpecificData CreateInstanceFunction<out TSpecificData>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where TSpecificData : TBaseData;
        
        private readonly Dictionary<IDataOwner, DataOwnerLookup<TWorldUniqueID, TBaseData>> m_DataOwnerLookups;
        private readonly Dictionary<TWorldUniqueID, TBaseData> m_WorldLookup;
        private readonly bool m_IsDataDisposable;

        public int Count
        {
            get => m_WorldLookup.Count;
        }

        public WorldDataOwnerLookup()
        {
            m_DataOwnerLookups = new Dictionary<IDataOwner, DataOwnerLookup<TWorldUniqueID, TBaseData>>();
            m_WorldLookup = new Dictionary<TWorldUniqueID, TBaseData>();
            m_IsDataDisposable = I_DISPOSABLE.IsAssignableFrom(typeof(TBaseData));
        }

        protected override void DisposeSelf()
        {
            if (m_IsDataDisposable)
            {
                foreach (IDisposable disposableData in m_WorldLookup.Values)
                {
                    disposableData.Dispose();
                }
            }
            m_WorldLookup.Clear();
            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // INIT
        //*************************************************************************************************************

        public void Add<TSpecificData>(TSpecificData data, IDataOwner dataOwner)
            where TSpecificData : TBaseData
        {
            DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup = GetOrCreateDataOwnerLookup(dataOwner);
            dataOwnerLookup.Add(data);
            m_WorldLookup.Add(data.WorldUniqueID, data);
        }

        public bool TryAdd<TSpecificData>(TSpecificData data, IDataOwner dataOwner)
            where TSpecificData : TBaseData
        {
            DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup = GetOrCreateDataOwnerLookup(dataOwner);
            if (!dataOwnerLookup.TryAdd(data))
            {
                return false;
            }
            m_WorldLookup.Add(data.WorldUniqueID, data);
            return true;
        }
        
        public TSpecificData Create<TSpecificData>(CreateInstanceFunction<TSpecificData> createInstanceFunction, IDataOwner dataOwner, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup = GetOrCreateDataOwnerLookup(dataOwner);
            TSpecificData data = dataOwnerLookup.Create(createInstanceFunction, uniqueContextIdentifier);
            m_WorldLookup.Add(data.WorldUniqueID, data);
            return data;
        }
        
        public TSpecificData GetOrCreate<TSpecificData>(TWorldUniqueID worldUniqueID, CreateInstanceFunction<TSpecificData> createInstanceFunction, IDataOwner dataOwner, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            if (!m_WorldLookup.TryGetValue(worldUniqueID, out TBaseData data))
            {
                data = Create(createInstanceFunction, dataOwner, uniqueContextIdentifier);
            }
            return (TSpecificData)data;
        }

        private DataOwnerLookup<TWorldUniqueID, TBaseData> GetOrCreateDataOwnerLookup(IDataOwner dataOwner)
        {
            if (!m_DataOwnerLookups.TryGetValue(dataOwner, out DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup))
            {
                dataOwnerLookup = new DataOwnerLookup<TWorldUniqueID, TBaseData>(dataOwner, m_IsDataDisposable);
                m_DataOwnerLookups.Add(dataOwner, dataOwnerLookup);
            }
            return dataOwnerLookup;
        }

        //*************************************************************************************************************
        // ACCESS
        //*************************************************************************************************************

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<KeyValuePair<TWorldUniqueID, TBaseData>> IEnumerable<KeyValuePair<TWorldUniqueID, TBaseData>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Dictionary<TWorldUniqueID, TBaseData>.Enumerator GetEnumerator()
        {
            return m_WorldLookup.GetEnumerator();
        }

        public bool TryGetData(TWorldUniqueID id, out TBaseData data)
        {
            return m_WorldLookup.TryGetValue(id, out data);
        }
        
    }
}
