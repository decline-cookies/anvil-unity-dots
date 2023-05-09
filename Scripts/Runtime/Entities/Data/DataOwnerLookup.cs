using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities
{
    internal class DataOwnerLookup<TWorldUniqueID, TBaseData> : AbstractAnvilBase
        where TBaseData : IWorldUniqueID<TWorldUniqueID>
    {
        public IDataOwner DataOwner { get; }

        private readonly Dictionary<int, TBaseData> m_InitLookup;
        private readonly Dictionary<TWorldUniqueID, TBaseData> m_HardenedLookup;
        private readonly bool m_IsDataDisposable;
        
        private bool m_IsHardened;

        public DataOwnerLookup(IDataOwner dataOwner, bool isDataDisposable) 
        {
            DataOwner = dataOwner;
            m_IsDataDisposable = isDataDisposable;
            m_InitLookup = new Dictionary<int, TBaseData>();
        }

        protected override void DisposeSelf()
        {
            if (m_IsDataDisposable)
            {
                foreach (IDisposable disposableData in m_InitLookup.Values)
                {
                    disposableData.Dispose();
                }
            }
            m_InitLookup.Clear();
            base.DisposeSelf();
        }
        
        //*************************************************************************************************************
        // INIT
        //*************************************************************************************************************

        private int GetInitID<TSpecificData>(string uniqueContextIdentifier)
        {
            Type type = typeof(TSpecificData);
            string initIDPath = $"{type.AssemblyQualifiedName}{uniqueContextIdentifier ?? string.Empty}";
            return initIDPath.GetBurstHashCode32();
        }

        public void InitAdd<TSpecificData>(TSpecificData data, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            Debug_EnsureNotHardened();
            int initID = GenerateAndCheckInitID<TSpecificData>(uniqueContextIdentifier);
            m_InitLookup.Add(initID, data);
        }

        public TSpecificData InitCreate<TSpecificData>(WorldDataOwnerLookup<TWorldUniqueID, TBaseData>.CreateInstanceFunction<TSpecificData> createInstanceFunction, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            Debug_EnsureNotHardened();

            int initID = GenerateAndCheckInitID<TSpecificData>(uniqueContextIdentifier);
            TSpecificData data = createInstanceFunction(DataOwner, uniqueContextIdentifier);
            m_InitLookup.Add(initID, data);
            return data;
        }

        private int GenerateAndCheckInitID<TSpecificData>(string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            int initID = GetInitID<TSpecificData>(uniqueContextIdentifier);
            int initIDNoType = GetInitID<TSpecificData>(string.Empty);

            //Check to see if we have anything already that matches this id
            if (m_InitLookup.ContainsKey(initID))
            {
                throw new InvalidOperationException($"Trying to add data of type {typeof(TSpecificData)}{(string.IsNullOrEmpty(uniqueContextIdentifier) ? string.Empty : $" with unique context of {uniqueContextIdentifier}")} to {DataOwner} but it already exists. Please ensure a unique context identifier.");
            }
            //If we have two or more of the same type, we want to ensure BOTH have unique context identifiers
            if (m_InitLookup.ContainsKey(initIDNoType))
            {
                throw new InvalidOperationException($"Trying to add data of type {typeof(TSpecificData)} with unique context of {uniqueContextIdentifier} but there is already another of the same type with no unique context identifier. Please ensure all data of the same type has a unique context identifier.");
            }

            return initID;
        }
        
        public TSpecificData InitGetOrCreate<TSpecificData>(WorldDataOwnerLookup<TWorldUniqueID, TBaseData>.CreateInstanceFunction<TSpecificData> createInstanceFunction, string uniqueContextIdentifier)
            where TSpecificData : TBaseData
        {
            Debug_EnsureNotHardened();
            int localID = GetInitID<TSpecificData>(uniqueContextIdentifier);

            if (!m_InitLookup.TryGetValue(localID, out TBaseData baseData))
            {
                baseData = InitCreate(createInstanceFunction, uniqueContextIdentifier);
            }
            return (TSpecificData)baseData;
        }
        
        //*************************************************************************************************************
        // HARDEN
        //*************************************************************************************************************

        public void Harden(Dictionary<TWorldUniqueID, TBaseData> worldHardenedLookup)
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;
            foreach (TBaseData data in m_InitLookup.Values)
            {
                TWorldUniqueID worldUniqueID = data.WorldUniqueID;
                m_HardenedLookup.Add(worldUniqueID, data);
                worldHardenedLookup.Add(worldUniqueID, data);
            }
            m_InitLookup.Clear();
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"{this} is already Hardened! It was not expected to be.");
            }
        }
    }
}
