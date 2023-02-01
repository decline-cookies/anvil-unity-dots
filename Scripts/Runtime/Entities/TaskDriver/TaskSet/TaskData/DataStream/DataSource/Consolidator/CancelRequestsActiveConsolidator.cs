using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    internal struct CancelRequestsActiveConsolidator
    {
        private const uint UNSET_COMPLETE_ACTIVE_ID = default;
        
        private readonly bool m_HasCancellableData;
        
        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_RequestLookup;
        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
        private readonly UnsafeTypedStream<EntityProxyInstanceWrapper<CancelCompleted>>.Writer m_CompleteWriter;
        private readonly uint m_CompleteActiveID;

        public CancelRequestsActiveConsolidator(UnsafeParallelHashMap<EntityProxyInstanceID, bool> requestLookup,
                                                ITaskSetOwner taskSetOwner)
        {
            m_RequestLookup = requestLookup;
            m_HasCancellableData = taskSetOwner.HasCancellableData;

            m_ProgressLookup = default;
            m_CompleteWriter = default;
            m_CompleteActiveID = UNSET_COMPLETE_ACTIVE_ID;

            if (m_HasCancellableData)
            {
                m_ProgressLookup = taskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData.Lookup;
            }
            else
            {
                CancelCompleteDataStream cancelCompleteDataStream = taskSetOwner.TaskSet.CancelCompleteDataStream;
                m_CompleteWriter = cancelCompleteDataStream.PendingWriter;
                m_CompleteActiveID = cancelCompleteDataStream.ActiveID;
            }
        }

        public void PrepareForConsolidation()
        {
            m_RequestLookup.Clear();
        }

        public void WriteToActive(EntityProxyInstanceID id, int laneIndex)
        {
            m_RequestLookup.TryAdd(id, true);

            if (m_HasCancellableData)
            {
                //We have something that wants to cancel, so we assume that it will get processed this frame.
                //If nothing processes it, it will auto-complete the next frame. 
                m_ProgressLookup.TryAdd(id, true);
            }
            else
            {
                UnsafeTypedStream<EntityProxyInstanceWrapper<CancelCompleted>>.LaneWriter completeLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);
                //Write ourselves to the Complete.
                CancelCompleted cancelCompleted = new CancelCompleted(id.Entity);
                completeLaneWriter.Write(new EntityProxyInstanceWrapper<CancelCompleted>(id.Entity, 
                                                                                         id.TaskSetOwnerID, 
                                                                                         m_CompleteActiveID, 
                                                                                         ref cancelCompleted));
            }
        }
    }
}
