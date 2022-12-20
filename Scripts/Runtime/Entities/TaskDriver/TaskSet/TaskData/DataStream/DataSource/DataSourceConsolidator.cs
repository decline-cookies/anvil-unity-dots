using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal struct DataSourceConsolidator<TInstance> : IDisposable
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
        private UnsafeParallelHashMap<uint, ActiveConsolidator<TInstance>> m_ActiveConsolidatorsByID;

        public unsafe DataSourceConsolidator(PendingData<TInstance> pendingData, Dictionary<uint, ActiveArrayData<TInstance>> dataMapping)
        {
            m_Pending = pendingData.Pending;
            
            m_ActiveConsolidatorsByID = new UnsafeParallelHashMap<uint, ActiveConsolidator<TInstance>>(dataMapping.Count,
                                                             Allocator.Persistent);
            foreach (KeyValuePair<uint, ActiveArrayData<TInstance>> entry in dataMapping)
            {
                void* activePointer = entry.Value.Active.GetBufferPointer();
                m_ActiveConsolidatorsByID.Add(entry.Key, new ActiveConsolidator<TInstance>(activePointer));
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
            foreach (KeyValue<uint, ActiveConsolidator<TInstance>> entry in m_ActiveConsolidatorsByID)
            {
                entry.Value.PrepareForConsolidation();
            }
            
            foreach (EntityProxyInstanceWrapper<TInstance> entry in m_Pending)
            {
                uint activeID = entry.InstanceID.ActiveID;
                ActiveConsolidator<TInstance> activeConsolidator = m_ActiveConsolidatorsByID[activeID];
                activeConsolidator.WritePending(entry);
            }

            m_Pending.Clear();
        }
    }
}
