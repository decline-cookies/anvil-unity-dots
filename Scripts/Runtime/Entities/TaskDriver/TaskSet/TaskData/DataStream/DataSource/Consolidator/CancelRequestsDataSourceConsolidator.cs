using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    internal struct CancelRequestsDataSourceConsolidator : IDisposable
    {
        private const int UNSET_THREAD_INDEX = -1;

        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

        private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
        private UnsafeParallelHashMap<DataTargetID, CancelRequestsActiveConsolidator> m_ActiveConsolidatorsByDataTargetID;

        public CancelRequestsDataSourceConsolidator(PendingData<EntityProxyInstanceID> pendingData, Dictionary<DataTargetID, AbstractData> dataMapping)
        {
            m_Pending = pendingData.Pending;
            m_ActiveConsolidatorsByDataTargetID
                = new UnsafeParallelHashMap<DataTargetID, CancelRequestsActiveConsolidator>(dataMapping.Count, Allocator.Persistent);
            foreach (KeyValuePair<DataTargetID, AbstractData> entry in dataMapping)
            {
                ActiveLookupData<EntityProxyInstanceID> activeLookupData = (ActiveLookupData<EntityProxyInstanceID>)entry.Value;
                m_ActiveConsolidatorsByDataTargetID.Add(
                    entry.Key,
                    new CancelRequestsActiveConsolidator(activeLookupData.Lookup, activeLookupData.TaskSetOwner));
            }

            m_NativeThreadIndex = UNSET_THREAD_INDEX;
        }

        public void Dispose()
        {
            if (m_ActiveConsolidatorsByDataTargetID.IsCreated)
            {
                m_ActiveConsolidatorsByDataTargetID.Dispose();
            }
        }

        public void Consolidate()
        {
            int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);

            foreach (KeyValue<DataTargetID, CancelRequestsActiveConsolidator> entry in m_ActiveConsolidatorsByDataTargetID)
            {
                entry.Value.PrepareForConsolidation();
            }

            foreach (EntityProxyInstanceID entry in m_Pending)
            {
                DataTargetID dataTargetID = entry.DataTargetID;
                CancelRequestsActiveConsolidator activeConsolidator = m_ActiveConsolidatorsByDataTargetID[dataTargetID];
                activeConsolidator.WriteToActive(entry, laneIndex);
            }

            m_Pending.Clear();
        }
    }
}