using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal struct CancelRequestsActiveConsolidator
    {
        private const uint UNSET_COMPLETE_ACTIVE_ID = default;
        
        private readonly bool m_HasCancellableData;
        
        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_RequestLookup;
        private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CompleteWriter;
        private readonly uint m_CompleteActiveID;

        public CancelRequestsActiveConsolidator(UnsafeParallelHashMap<EntityProxyInstanceID, bool> requestLookup,
                                                ITaskSetOwner taskSetOwner)
        {
            m_RequestLookup = requestLookup;
            m_HasCancellableData = taskSetOwner.HasCancellableData;
            
            m_CompleteWriter = default;
            m_CompleteActiveID = UNSET_COMPLETE_ACTIVE_ID;

            if (m_HasCancellableData)
            {
                //TODO: Get progress stuff
            }
            else
            {
                CancelCompleteDataStream cancelCompleteDataStream = taskSetOwner.TaskSet.CancelCompleteDataStream;
                m_CompleteWriter = cancelCompleteDataStream.PendingWriter;
                m_CompleteActiveID = cancelCompleteDataStream.GetActiveID();
            }
        }

        public void PrepareForConsolidation()
        {
            m_RequestLookup.Clear();
        }

        public void WriteToActive(EntityProxyInstanceID id, int laneIndex)
        {
            //TODO: Handle the cases where we don't have cancellable data
            //TODO: Write to the Progress lookup at the same time
            m_RequestLookup.TryAdd(id, true);

            if (m_HasCancellableData)
            {
                
            }
            else
            {
                UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter completeLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);
                completeLaneWriter.Write(new EntityProxyInstanceID(id.Entity, id.TaskSetOwnerID, m_CompleteActiveID));
            }
        }
    }
}
