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
        private UnsafeParallelHashMap<DataTargetID, EntityProxyActiveConsolidator<TInstance>> m_ActiveConsolidatorsByDataTargetID;

        public EntityProxyDataSourceConsolidator(
            PendingData<EntityProxyInstanceWrapper<TInstance>> pendingData,
            Dictionary<DataTargetID, AbstractData> dataMapping)
        {
            m_Pending = pendingData.Pending;

            m_ActiveConsolidatorsByDataTargetID
                = new UnsafeParallelHashMap<DataTargetID, EntityProxyActiveConsolidator<TInstance>>(dataMapping.Count, Allocator.Persistent);
            foreach (KeyValuePair<DataTargetID, AbstractData> entry in dataMapping)
            {
                ActiveArrayData<EntityProxyInstanceWrapper<TInstance>> activeArrayData = (ActiveArrayData<EntityProxyInstanceWrapper<TInstance>>)entry.Value;
                m_ActiveConsolidatorsByDataTargetID.Add(entry.Key, new EntityProxyActiveConsolidator<TInstance>(activeArrayData));
            }
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
            foreach (KeyValue<DataTargetID, EntityProxyActiveConsolidator<TInstance>> entry in m_ActiveConsolidatorsByDataTargetID)
            {
                entry.Value.PrepareForConsolidation();
            }

            foreach (EntityProxyInstanceWrapper<TInstance> entry in m_Pending)
            {
                DataTargetID dataTargetID = entry.InstanceID.DataTargetID;
                EntityProxyActiveConsolidator<TInstance> entityProxyActiveConsolidator = m_ActiveConsolidatorsByDataTargetID[dataTargetID];
                entityProxyActiveConsolidator.WriteToActive(entry);
            }

            m_Pending.Clear();
        }
    }
}