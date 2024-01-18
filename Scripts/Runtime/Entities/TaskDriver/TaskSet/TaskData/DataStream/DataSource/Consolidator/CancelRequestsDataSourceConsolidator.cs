using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal struct CancelRequestsDataSourceConsolidator : IDisposable
    {
        private const int UNSET_THREAD_INDEX = -1;

        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

        private UnsafeTypedStream<EntityKeyedTaskID> m_Pending;
        private UnsafeParallelHashMap<DataTargetID, CancelRequestsActiveConsolidator> m_ActiveConsolidatorsByDataTargetID;

        public CancelRequestsDataSourceConsolidator(PendingData<EntityKeyedTaskID> pendingData, HashSet<AbstractData> dataTargets)
        {
            m_Pending = pendingData.Pending;
            m_ActiveConsolidatorsByDataTargetID
                = new UnsafeParallelHashMap<DataTargetID, CancelRequestsActiveConsolidator>(dataTargets.Count, Allocator.Persistent);
            foreach (AbstractData dataTarget in dataTargets)
            {
                if (dataTarget is not ActiveLookupData<EntityKeyedTaskID> activeLookupData)
                {
                    continue;
                }
                m_ActiveConsolidatorsByDataTargetID.Add(
                    activeLookupData.WorldUniqueID,
                    new CancelRequestsActiveConsolidator(activeLookupData.Lookup, (ITaskSetOwner)activeLookupData.DataOwner));
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

            foreach (EntityKeyedTaskID entry in m_Pending)
            {
                DataTargetID dataTargetID = entry.DataTargetID;
                CancelRequestsActiveConsolidator activeConsolidator = m_ActiveConsolidatorsByDataTargetID[dataTargetID];
                activeConsolidator.WriteToActive(entry, laneIndex);
            }

            m_Pending.Clear();
        }
    }
}