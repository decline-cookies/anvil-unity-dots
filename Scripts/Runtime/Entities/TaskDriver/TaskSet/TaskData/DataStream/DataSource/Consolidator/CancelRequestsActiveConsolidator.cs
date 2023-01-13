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
        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
        private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CompleteWriter;
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
                m_ProgressLookup = taskSetOwner.TaskSet.CancelRequestsDataStream.ProgressLookupData.Lookup;
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
            //TODO: There is a disconnect here between the System ID and the TaskDriver ID for who owns this.
            //TODO: This also raises an issue with if the ProgressLookup will ever get properly cleared.
            
            UnityEngine.Debug.Log($"Requesting Cancel for - {id}");
            m_RequestLookup.TryAdd(id, true);

            if (m_HasCancellableData)
            {
                UnityEngine.Debug.Log($"Adding to Progress Lookup - {id}");
                //We have something that wants to cancel, so we assume that it will get processed this frame.
                //If nothing processes it, it will auto-complete the next frame. 
                m_ProgressLookup.TryAdd(id, true);
            }
            else
            {
                UnityEngine.Debug.Log($"Directly completing for - {id}");
                UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter completeLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);
                completeLaneWriter.Write(new EntityProxyInstanceID(id.Entity, id.TaskSetOwnerID, m_CompleteActiveID));
            }
        }
    }
}
