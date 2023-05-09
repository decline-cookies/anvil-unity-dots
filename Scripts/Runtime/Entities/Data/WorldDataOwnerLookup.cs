using Anvil.CSharp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities
{
    internal class WorldDataOwnerLookup<TWorldUniqueID, TBaseData> : AbstractAnvilBase,
                                                                     IEnumerable<KeyValuePair<TWorldUniqueID, TBaseData>> 
        where TBaseData : IWorldUniqueID<TWorldUniqueID>
    {
        private static readonly Type I_DISPOSABLE = typeof(IDisposable);
        
        public delegate TSpecificData CreateInstanceFunction<out TSpecificData>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where TSpecificData : TBaseData;
        
        private readonly Dictionary<IDataOwner, DataOwnerLookup<TWorldUniqueID, TBaseData>> m_InitLookups;
        private readonly Dictionary<TWorldUniqueID, TBaseData> m_HardenedLookup;
        private readonly bool m_IsDataDisposable;

        public bool IsHardened { get; private set; }

        public int Count
        {
            get
            {
                Debug_EnsureHardened();
                return m_HardenedLookup.Count;
            }
        }

        public WorldDataOwnerLookup()
        {
            m_InitLookups = new Dictionary<IDataOwner, DataOwnerLookup<TWorldUniqueID, TBaseData>>();
            m_HardenedLookup = new Dictionary<TWorldUniqueID, TBaseData>();
            m_IsDataDisposable = I_DISPOSABLE.IsAssignableFrom(typeof(TBaseData));
        }

        protected override void DisposeSelf()
        {
            if (m_IsDataDisposable)
            {
                foreach (IDisposable disposableData in m_HardenedLookup.Values)
                {
                    disposableData.Dispose();
                }
            }
            m_HardenedLookup.Clear();
            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // INIT
        //*************************************************************************************************************

        public void InitAdd<TSpecificData>(TSpecificData data, IDataOwner dataOwner, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            Debug_EnsureHardened();
            DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup = InitGetOrCreate(dataOwner);
            dataOwnerLookup.InitAdd(data, uniqueContextIdentifier);
        }
        
        public TSpecificData InitCreate<TSpecificData>(CreateInstanceFunction<TSpecificData> createInstanceFunction, IDataOwner dataOwner, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            Debug_EnsureNotHardened();
            DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup = InitGetOrCreate(dataOwner);
            return dataOwnerLookup.InitCreate(createInstanceFunction, uniqueContextIdentifier);
        }
        
        public TSpecificData InitGetOrCreate<TSpecificData>(CreateInstanceFunction<TSpecificData> createInstanceFunction, IDataOwner dataOwner, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            Debug_EnsureNotHardened();
            DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup = InitGetOrCreate(dataOwner);
            return dataOwnerLookup.InitGetOrCreate(createInstanceFunction, uniqueContextIdentifier);
        }

        private DataOwnerLookup<TWorldUniqueID, TBaseData> InitGetOrCreate(IDataOwner dataOwner)
        {
            Debug_EnsureNotHardened();
            if (!m_InitLookups.TryGetValue(dataOwner, out DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup))
            {
                dataOwnerLookup = new DataOwnerLookup<TWorldUniqueID, TBaseData>(dataOwner, m_IsDataDisposable);
                m_InitLookups.Add(dataOwner, dataOwnerLookup);
            }
            return dataOwnerLookup;
        }
        
        //*************************************************************************************************************
        // HARDENING
        //*************************************************************************************************************
        public void Harden()
        {
            Debug_EnsureNotHardened();
            IsHardened = true;
            foreach (DataOwnerLookup<TWorldUniqueID, TBaseData> dataOwnerLookup in m_InitLookups.Values)
            {
                dataOwnerLookup.Harden(m_HardenedLookup);
            }
            
            m_InitLookups.Clear();
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
            Debug_EnsureHardened();
            return m_HardenedLookup.GetEnumerator();
        }

        public bool TryGetData(TWorldUniqueID id, out TBaseData data)
        {
            Debug_EnsureHardened();
            return m_HardenedLookup.TryGetValue(id, out data);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureNotHardened()
        {
            if (IsHardened)
            {
                throw new InvalidOperationException($"{this} is already Hardened! It was not expected to be.");
            }
        }
        
        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureHardened()
        {
            if (!IsHardened)
            {
                throw new InvalidOperationException($"{this} isn't Hardened! It should be before calling the caller.");
            }
        }
    }
}
