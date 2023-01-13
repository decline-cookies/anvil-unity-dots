using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelProgressFlowNode : AbstractAnvilBase
    {
        public static readonly BulkScheduleDelegate<CancelProgressFlowNode> CHECK_PROGRESS_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<CancelProgressFlowNode>(nameof(ScheduleCheckCancelProgressJob), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly ITaskSetOwner m_TaskSetOwner;
        private readonly CancelProgressFlowNode m_Parent;
        private readonly ActiveLookupData<EntityProxyInstanceID> m_ProgressLookupData;
        private readonly ActiveLookupData<EntityProxyInstanceID> m_ParentProgressLookupData;
        private readonly PendingData<EntityProxyInstanceID> m_CancelCompleteData;
        private readonly uint m_CancelCompleteActiveID;

        private NativeArray<JobHandle> m_Dependencies;


        public CancelProgressFlowNode(ITaskSetOwner taskSetOwner, CancelProgressFlowNode parent)
        {
            m_TaskSetOwner = taskSetOwner;
            m_Parent = parent;

            m_ProgressLookupData = m_TaskSetOwner.TaskSet.CancelRequestsDataStream.ProgressLookupData;
            m_CancelCompleteData = m_TaskSetOwner.TaskSet.CancelCompleteDataStream.PendingData;
            m_CancelCompleteActiveID = m_TaskSetOwner.TaskSet.CancelCompleteDataStream.GetActiveID();


            m_Dependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);

            if (m_Parent != null)
            {
                m_ParentProgressLookupData = m_Parent.m_TaskSetOwner.TaskSet.CancelRequestsDataStream.ProgressLookupData;
            }
        }

        protected override void DisposeSelf()
        {
            m_Dependencies.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{m_TaskSetOwner} - With CancelRequestData of {m_TaskSetOwner.TaskSet.CancelRequestsDataStream.GetActiveID()} and CancelProgressData of {m_TaskSetOwner.TaskSet.CancelRequestsDataStream.ProgressLookupData.ID} and CancelCompleteData of {m_TaskSetOwner.TaskSet.CancelCompleteDataStream.GetActiveID()}";
        }

        private JobHandle ScheduleCheckCancelProgressJob(JobHandle dependsOn)
        {
            m_Dependencies[0] = dependsOn;
            m_Dependencies[1] = m_ProgressLookupData.AcquireAsync(AccessType.ExclusiveWrite);
            m_Dependencies[2] = m_CancelCompleteData.AcquireAsync(AccessType.SharedWrite);
            m_Dependencies[3] = (m_Parent != null)
                ? m_ParentProgressLookupData.AcquireAsync(AccessType.ExclusiveWrite)
                : default;

            dependsOn = JobHandle.CombineDependencies(m_Dependencies);

            CheckCancelProgressJob checkCancelProgressJob = new CheckCancelProgressJob(m_ProgressLookupData.Lookup,
                                                                                       m_CancelCompleteData.PendingWriter,
                                                                                       m_CancelCompleteActiveID,
                                                                                       m_Parent != null
                                                                                           ? m_ParentProgressLookupData.Lookup
                                                                                           : default,
                                                                                       m_Parent != null
                                                                                           ? m_ParentProgressLookupData.TaskSetOwner.ID
                                                                                           : 0);

            dependsOn = checkCancelProgressJob.Schedule(dependsOn);

            m_ProgressLookupData.ReleaseAsync(dependsOn);
            m_CancelCompleteData.ReleaseAsync(dependsOn);
            if (m_Parent != null)
            {
                m_ParentProgressLookupData.ReleaseAsync(dependsOn);
            }

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct CheckCancelProgressJob : IJob
        {
            private const int UNSET_THREAD_INDEX = -1;

            [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
            [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CompleteWriter;
            private readonly uint m_CancelCompleteActiveID;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ParentProgressLookup;
            private readonly uint m_ParentTaskSetOwnerID;

            private UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter m_CompleteLaneWriter;

            public CheckCancelProgressJob(UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup,
                                          UnsafeTypedStream<EntityProxyInstanceID>.Writer completeWriter,
                                          uint cancelCompleteActiveID,
                                          UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup,
                                          uint parentTaskSetOwnerID)
            {
                m_ProgressLookup = progressLookup;
                m_CompleteWriter = completeWriter;
                m_CancelCompleteActiveID = cancelCompleteActiveID;
                m_ParentProgressLookup = parentProgressLookup;
                m_ParentTaskSetOwnerID = parentTaskSetOwnerID;

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
                m_CompleteLaneWriter = default;
            }

            public void Execute()
            {
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                m_CompleteLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);

                foreach (KeyValue<EntityProxyInstanceID, bool> entry in m_ProgressLookup)
                {
                    EntityProxyInstanceID id = entry.Key;
                    ref bool isStillProcessing = ref entry.Value;
                    //If we're still processing we'll allow a Cancel Job to occur
                    if (isStillProcessing)
                    {
                        UnityEngine.Debug.Log($"Still processing for {id.ToFixedString()} - Holding open");
                        //Flip us back to not processing. A CancelJob will switch this if we still need to process
                        isStillProcessing = false;
                        //If we have a parent, we need to hold it open until we complete
                        if (m_ParentProgressLookup.IsCreated)
                        {
                            EntityProxyInstanceID parentID = new EntityProxyInstanceID(id, m_ParentTaskSetOwnerID);
                            m_ParentProgressLookup[parentID] = true;
                        }
                    }
                    //If we're not processing then:
                    // - All Cancel Jobs are complete 
                    // OR
                    // - There never were any Cancel Jobs to begin with
                    // OR
                    // - There wasn't any data for this id that was requested to cancel.
                    else
                    {
                        UnityEngine.Debug.Log($"No longer processing for {id.ToFixedString()} - Completing");
                        //Remove ourselves from the Progress Lookup
                        m_ProgressLookup.Remove(id);
                        //Write ourselves to the Complete.
                        m_CompleteLaneWriter.Write(new EntityProxyInstanceID(id.Entity, id.TaskSetOwnerID, m_CancelCompleteActiveID));
                    }
                }
            }
        }
    }
}
