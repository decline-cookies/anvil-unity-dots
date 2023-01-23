using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    internal struct CancelCompleteDataSourceConsolidator : IDisposable
    {
        private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
        private UnsafeParallelHashMap<uint, CancelCompleteActiveConsolidator> m_ActiveConsolidatorsByID;

        public unsafe CancelCompleteDataSourceConsolidator(PendingData<EntityProxyInstanceID> pendingData, Dictionary<uint, AbstractData> dataMapping) : this()
        {
            m_Pending = pendingData.Pending;
            m_ActiveConsolidatorsByID = new UnsafeParallelHashMap<uint, CancelCompleteActiveConsolidator>(dataMapping.Count, Allocator.Persistent);
            foreach (KeyValuePair<uint, AbstractData> entry in dataMapping)
            {
                ActiveArrayData<EntityProxyInstanceID> activeArrayData = (ActiveArrayData<EntityProxyInstanceID>)entry.Value;
                void* activePointer = activeArrayData.Active.GetBufferPointer();
                m_ActiveConsolidatorsByID.Add(entry.Key, new CancelCompleteActiveConsolidator(activePointer));
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
            foreach (KeyValue<uint, CancelCompleteActiveConsolidator> entry in m_ActiveConsolidatorsByID)
            {
                entry.Value.PrepareForConsolidation();
            }

            foreach (EntityProxyInstanceID id in m_Pending)
            {
                CancelCompleteActiveConsolidator cancelCompleteActiveConsolidator = m_ActiveConsolidatorsByID[id.ActiveID];
                cancelCompleteActiveConsolidator.WriteToActive(id);
            }

            m_Pending.Clear();
        }
    }
}
