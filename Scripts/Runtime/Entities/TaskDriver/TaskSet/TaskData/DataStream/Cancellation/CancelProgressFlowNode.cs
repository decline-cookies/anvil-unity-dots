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
        private readonly uint m_ParentTaskSetOwnerID;
        private readonly uint m_CancelCompleteActiveID;

        private NativeArray<JobHandle> m_Dependencies;

        public CancelProgressFlowNode(ITaskSetOwner taskSetOwner, CancelProgressFlowNode parent)
        {
            m_TaskSetOwner = taskSetOwner;
            m_Parent = parent;

            m_ProgressLookupData = m_TaskSetOwner.TaskSet.CancelRequestsDataStream.ProgressLookupData;
            m_CancelCompleteData = m_TaskSetOwner.TaskSet.CancelCompleteDataStream.PendingData;
            m_CancelCompleteActiveID = m_TaskSetOwner.TaskSet.CancelCompleteDataStream.GetActiveID();

            if (m_Parent == null)
            {
                m_Dependencies = new NativeArray<JobHandle>(3, Allocator.Persistent);
            }
            else
            {
                m_Dependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);
                m_ParentProgressLookupData = m_Parent.m_TaskSetOwner.TaskSet.CancelRequestsDataStream.ProgressLookupData;
                m_ParentTaskSetOwnerID = m_Parent.m_TaskSetOwner.ID;
            }
        }

        protected override void DisposeSelf()
        {
            m_Dependencies.Dispose();
            base.DisposeSelf();
        }
        
        private JobHandle ScheduleCheckCancelProgressJob(JobHandle dependsOn)
        {
            return m_Parent == null
                ? ScheduleCheckCancelProgressJobWithoutParent(dependsOn)
                : ScheduleCheckCancelProgressJobWithParent(dependsOn);
        }

        private JobHandle ScheduleCheckCancelProgressJobWithParent(JobHandle dependsOn)
        {
            m_Dependencies[0] = dependsOn;
            m_Dependencies[1] = m_ProgressLookupData.AcquireAsync(AccessType.ExclusiveWrite);
            m_Dependencies[2] = m_CancelCompleteData.AcquireAsync(AccessType.SharedWrite);
            m_Dependencies[3] = m_ParentProgressLookupData.AcquireAsync(AccessType.ExclusiveWrite);

            dependsOn = JobHandle.CombineDependencies(m_Dependencies);

            CheckCancelProgressJobWithParent checkCancelProgressJobWithParent = new CheckCancelProgressJobWithParent(m_ParentProgressLookupData.Lookup,
                                                                                                                     m_ProgressLookupData.Lookup,
                                                                                                                     m_CancelCompleteData.PendingWriter,
                                                                                                                     m_CancelCompleteActiveID,
                                                                                                                     m_ParentTaskSetOwnerID);

            dependsOn = checkCancelProgressJobWithParent.Schedule(dependsOn);

            m_ProgressLookupData.ReleaseAsync(dependsOn);
            m_CancelCompleteData.ReleaseAsync(dependsOn);
            m_ParentProgressLookupData.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        private JobHandle ScheduleCheckCancelProgressJobWithoutParent(JobHandle dependsOn)
        {
            m_Dependencies[0] = dependsOn;
            m_Dependencies[1] = m_ProgressLookupData.AcquireAsync(AccessType.ExclusiveWrite);
            m_Dependencies[2] = m_CancelCompleteData.AcquireAsync(AccessType.SharedWrite);

            dependsOn = JobHandle.CombineDependencies(m_Dependencies);

            CheckCancelProgressJobWithoutParent checkCancelProgressJobWithoutParent = new CheckCancelProgressJobWithoutParent(m_ProgressLookupData.Lookup,
                                                                                                                              m_CancelCompleteData.PendingWriter,
                                                                                                                              m_CancelCompleteActiveID);

            dependsOn = checkCancelProgressJobWithoutParent.Schedule(dependsOn);
            
            m_ProgressLookupData.ReleaseAsync(dependsOn);
            m_CancelCompleteData.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct CheckCancelProgressJobWithParent : IJob
        {
            private const int UNSET_THREAD_INDEX = -1;

            [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ParentProgressLookup;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
            [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CompleteWriter;
            private readonly uint m_CancelCompleteActiveID;
            private readonly uint m_ParentTaskSetOwnerID;

            private UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter m_CompleteLaneWriter;

            public CheckCancelProgressJobWithParent(UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup,
                                                    UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup,
                                                    UnsafeTypedStream<EntityProxyInstanceID>.Writer completeWriter,
                                                    uint cancelCompleteActiveID,
                                                    uint parentTaskSetOwnerID)
            {
                m_ParentProgressLookup = parentProgressLookup;
                m_ProgressLookup = progressLookup;
                m_CompleteWriter = completeWriter;
                m_CancelCompleteActiveID = cancelCompleteActiveID;
                m_ParentTaskSetOwnerID = parentTaskSetOwnerID;

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
                m_CompleteLaneWriter = default;
            }

            public void Execute()
            {
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                m_CompleteLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);

                //Go through all entries in the parent lookup. 
                //If we are a System, our parent must be a TaskDriver.
                //Therefore we could have have entries from multiple TaskDrivers but we only want to process 
                //one in this job for context and completion bubble up purposes
                //If we are a TaskDriver, then our parent must also still be a TaskDriver and the logic holds.
                foreach (KeyValue<EntityProxyInstanceID, bool> entry in m_ParentProgressLookup)
                {
                    EntityProxyInstanceID parentID = entry.Key;
                    ref bool isParentProcessing = ref entry.Value;
                    EntityProxyInstanceID id = new EntityProxyInstanceID(parentID.Entity, m_ParentTaskSetOwnerID, m_CancelCompleteActiveID);

                    bool isStillProcessing = m_ProgressLookup[id];
                    HandleProgress(isStillProcessing, ref id);
                    if (isParentProcessing)
                    {
                        //Hold open the parent, the parent shouldn't collapse until nothing is holding it open
                        isParentProcessing = true;
                    }
                }
            }

            private void HandleProgress(bool isStillProcessing, ref EntityProxyInstanceID id)
            {
                //If we're still processing...
                if (isStillProcessing)
                {
                    UnityEngine.Debug.Log($"Still processing for {id.ToFixedString()} - Holding open");
                    //Flip us back to not processing. A CancelJob will switch this if we still need to process
                    m_ProgressLookup[id] = false;
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
                    m_CompleteLaneWriter.Write(id);
                }
            }
        }

        [BurstCompile]
        private struct CheckCancelProgressJobWithoutParent : IJob
        {
            private const int UNSET_THREAD_INDEX = -1;

            [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
            [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CompleteWriter;

            private UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter m_CompleteLaneWriter;
            private readonly uint m_CancelCompleteActiveID;

            public CheckCancelProgressJobWithoutParent(UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup,
                                                       UnsafeTypedStream<EntityProxyInstanceID>.Writer completeWriter,
                                                       uint cancelCompleteActiveID)
            {
                m_ProgressLookup = progressLookup;
                m_CompleteWriter = completeWriter;
                m_CancelCompleteActiveID = cancelCompleteActiveID;

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
                m_CompleteLaneWriter = default;
            }

            public void Execute()
            {
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                m_CompleteLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);

                //We don't have a parent so we must be the top level TaskDriver.
                //We need to loop through ourselves instead.
                foreach (KeyValue<EntityProxyInstanceID, bool> entry in m_ProgressLookup)
                {
                    EntityProxyInstanceID id = entry.Key;
                    HandleProgress(entry.Value, ref id);
                }
            }

            //TODO: Static and share between jobs
            private void HandleProgress(bool isStillProcessing, ref EntityProxyInstanceID id)
            {
                //If we're still processing...
                if (isStillProcessing)
                {
                    UnityEngine.Debug.Log($"TOP LEVEL - Still processing for {id.ToFixedString()} - Holding open");
                    //Flip us back to not processing. A CancelJob will switch this if we still need to process
                    m_ProgressLookup[id] = false;
                }
                //If we're not processing then:
                // - All Cancel Jobs are complete 
                // OR
                // - There never were any Cancel Jobs to begin with
                // OR
                // - There wasn't any data for this id that was requested to cancel.
                else
                {
                    UnityEngine.Debug.Log($"TOP LEVEL - No longer processing for {id.ToFixedString()} - Completing");
                    //Remove ourselves from the Progress Lookup
                    m_ProgressLookup.Remove(id);
                    //Write ourselves to the Complete.
                    m_CompleteLaneWriter.Write(new EntityProxyInstanceID(id.Entity, id.TaskSetOwnerID, m_CancelCompleteActiveID));
                }
            }
        }
    }
}
