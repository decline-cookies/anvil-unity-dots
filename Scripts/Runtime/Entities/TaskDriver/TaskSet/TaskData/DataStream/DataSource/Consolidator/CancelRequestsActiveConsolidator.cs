using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal struct CancelRequestsActiveConsolidator
    {
        private readonly bool m_HasCancellableData;

        private UnsafeParallelHashMap<EntityKeyedTaskID, bool> m_RequestLookup;
        private UnsafeParallelHashMap<EntityKeyedTaskID, bool> m_ProgressLookup;
        private readonly UnsafeTypedStream<EntityKeyedTaskWrapper<CancelComplete>>.Writer m_CompleteWriter;
        private readonly DataTargetID m_CompleteDataTargetID;

        public CancelRequestsActiveConsolidator(
            UnsafeParallelHashMap<EntityKeyedTaskID, bool> requestLookup,
            ITaskSetOwner taskSetOwner) : this()
        {
            m_RequestLookup = requestLookup;
            m_HasCancellableData = taskSetOwner.HasCancellableData;

            m_ProgressLookup = default;
            m_CompleteWriter = default;

            if (m_HasCancellableData)
            {
                m_ProgressLookup = taskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData.Lookup;
            }
            else
            {
                CancelCompleteDataStream cancelCompleteDataStream = taskSetOwner.TaskSet.CancelCompleteDataStream;
                m_CompleteWriter = cancelCompleteDataStream.PendingWriter;
                m_CompleteDataTargetID = cancelCompleteDataStream.DataTargetID;
            }
        }

        public void PrepareForConsolidation()
        {
            m_RequestLookup.Clear();
        }

        public void WriteToActive(EntityKeyedTaskID id, int laneIndex)
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
                UnsafeTypedStream<EntityKeyedTaskWrapper<CancelComplete>>.LaneWriter completeLaneWriter
                    = m_CompleteWriter.AsLaneWriter(laneIndex);
                //Write ourselves to the Complete.
                CancelComplete cancelComplete = new CancelComplete(id.Entity);
                completeLaneWriter.Write(
                    new EntityKeyedTaskWrapper<CancelComplete>(
                        id.Entity,
                        id.DataOwnerID,
                        m_CompleteDataTargetID,
                        ref cancelComplete));
            }
        }
    }
}