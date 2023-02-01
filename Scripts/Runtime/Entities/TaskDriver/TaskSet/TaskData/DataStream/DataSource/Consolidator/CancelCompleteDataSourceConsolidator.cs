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
        private UnsafeTypedStream<EntityProxyInstanceWrapper<CancelCompleted>> m_Pending;
        private UnsafeParallelHashMap<uint, CancelCompleteActiveConsolidator> m_ActiveConsolidatorsByID;

        public unsafe CancelCompleteDataSourceConsolidator(PendingData<EntityProxyInstanceWrapper<CancelCompleted>> pendingData, Dictionary<uint, AbstractData> dataMapping) : this()
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

            foreach (EntityProxyInstanceWrapper<CancelCompleted> wrapper in m_Pending)
            {
                CancelCompleteActiveConsolidator cancelCompleteActiveConsolidator = m_ActiveConsolidatorsByID[wrapper.InstanceID.ActiveID];
                cancelCompleteActiveConsolidator.WriteToActive(wrapper);
            }

            m_Pending.Clear();
        }
    }
}
