using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #108 - Add profiling and debug string information, see if can be done without a ton of #IF Checks
    //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/105#discussion_r1043567688
    //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/105#discussion_r1043573642
    
    [BurstCompatible]
    internal struct EntityProxyDataSourceConsolidator<TInstance> : IDisposable
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
        private UnsafeParallelHashMap<uint, EntityProxyActiveConsolidator<TInstance>> m_ActiveConsolidatorsByID;

        public EntityProxyDataSourceConsolidator(PendingData<EntityProxyInstanceWrapper<TInstance>> pendingData,
                                                        Dictionary<uint, AbstractData> dataMapping)
        {
            m_Pending = pendingData.Pending;

            m_ActiveConsolidatorsByID = new UnsafeParallelHashMap<uint, EntityProxyActiveConsolidator<TInstance>>(dataMapping.Count, Allocator.Persistent);
            foreach (KeyValuePair<uint, AbstractData> entry in dataMapping)
            {
                ActiveArrayData<EntityProxyInstanceWrapper<TInstance>> activeArrayData = (ActiveArrayData<EntityProxyInstanceWrapper<TInstance>>)entry.Value;
                m_ActiveConsolidatorsByID.Add(entry.Key, new EntityProxyActiveConsolidator<TInstance>(activeArrayData));
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
            foreach (KeyValue<uint, EntityProxyActiveConsolidator<TInstance>> entry in m_ActiveConsolidatorsByID)
            {
                entry.Value.PrepareForConsolidation();
            }

            foreach (EntityProxyInstanceWrapper<TInstance> entry in m_Pending)
            {
                uint activeID = entry.InstanceID.ActiveID;
                EntityProxyActiveConsolidator<TInstance> entityProxyActiveConsolidator = m_ActiveConsolidatorsByID[activeID];
                entityProxyActiveConsolidator.WriteToActive(entry);
            }

            m_Pending.Clear();
        }
    }
}
