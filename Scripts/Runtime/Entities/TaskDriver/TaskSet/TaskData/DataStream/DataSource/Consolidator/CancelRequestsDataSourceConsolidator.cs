using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal struct CancelRequestsDataSourceConsolidator : IDisposable
    {
        private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
        private UnsafeParallelHashMap<uint, CancelRequestsActiveConsolidator> m_ActiveConsolidatorsByID;

        public CancelRequestsDataSourceConsolidator(PendingData<EntityProxyInstanceID> pendingData, Dictionary<uint, AbstractData> dataMapping)
        {
            m_Pending = pendingData.Pending;
            m_ActiveConsolidatorsByID = new UnsafeParallelHashMap<uint, CancelRequestsActiveConsolidator>(dataMapping.Count, Allocator.Persistent);
            foreach (KeyValuePair<uint, AbstractData> entry in dataMapping)
            {
                ActiveLookupData<EntityProxyInstanceID> activeLookupData = (ActiveLookupData<EntityProxyInstanceID>)entry.Value;
                m_ActiveConsolidatorsByID.Add(entry.Key, new CancelRequestsActiveConsolidator(activeLookupData.Lookup));
            }
        }

        public void Dispose()
        {
            if (m_ActiveConsolidatorsByID.IsCreated)
            {
                m_ActiveConsolidatorsByID.Dispose();
            }
        }

        public void Consolidate()
        {
            foreach (KeyValue<uint, CancelRequestsActiveConsolidator> entry in m_ActiveConsolidatorsByID)
            {
                entry.Value.PrepareForConsolidation();
            }

            foreach (EntityProxyInstanceID entry in m_Pending)
            {
                uint activeID = entry.ActiveID;
                CancelRequestsActiveConsolidator activeConsolidator = m_ActiveConsolidatorsByID[activeID];
                activeConsolidator.WriteToActive(entry);
            }

            m_Pending.Clear();
        }
    }
}
