using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressFlowNode : AbstractAnvilBase
    {
        public static readonly BulkScheduleDelegate<CancelProgressFlowNode> CHECK_PROGRESS_SCHEDULE_FUNCTION
            = BulkSchedulingUtil.CreateSchedulingDelegate<CancelProgressFlowNode>(
                nameof(ScheduleCheckProgressJob),
                BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly ITaskSetOwner m_TaskSetOwner;
        private readonly CancelProgressFlowNode m_Parent;
        private readonly ActiveLookupData<EntityKeyedTaskID> m_RequestLookupData;
        private readonly ActiveLookupData<EntityKeyedTaskID> m_ProgressLookupData;
        private readonly PendingData<EntityKeyedTaskWrapper<CancelComplete>> m_CancelCompleteData;
        private readonly DataTargetID m_CancelCompleteDataTargetID;

        private readonly OwnerSharedCheckProgressState m_LastCheckProgressState;
        private NativeArray<JobHandle> m_Dependencies;


        public CancelProgressFlowNode(ITaskSetOwner taskSetOwner, CancelProgressFlowNode parent)
        {
            m_TaskSetOwner = taskSetOwner;
            m_Parent = parent;

            m_RequestLookupData = m_TaskSetOwner.TaskSet.CancelRequestsDataStream.ActiveLookupData;
            m_ProgressLookupData = m_TaskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData;
            m_CancelCompleteData = m_TaskSetOwner.TaskSet.CancelCompleteDataStream.PendingData;
            m_CancelCompleteDataTargetID = m_TaskSetOwner.TaskSet.CancelCompleteDataStream.DataTargetID;

            m_LastCheckProgressState = new OwnerSharedCheckProgressState(m_TaskSetOwner);
            m_Dependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            m_LastCheckProgressState.Dispose();
            m_Dependencies.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{m_TaskSetOwner} - With CancelRequestData of {m_TaskSetOwner.TaskSet.CancelRequestsDataStream.DataTargetID} and CancelProgressData of {m_TaskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData.WorldUniqueID} and CancelCompleteData of {m_TaskSetOwner.TaskSet.CancelCompleteDataStream.DataTargetID}";
        }

        private bool IsProgressLookupDataInvalidated()
        {
            return m_ProgressLookupData.IsDataInvalidated(m_LastCheckProgressState.ProgressLookupDataVersion);
        }

        private JobHandle ScheduleCheckProgressJob(JobHandle dependsOn)
        {
            bool hasOurDataChanged = IsProgressLookupDataInvalidated();
            bool hasParentDataChanged = m_Parent?.IsProgressLookupDataInvalidated() ?? false;

            //If nothing changed, and no follow up is required we don't need to schedule
            if (!hasOurDataChanged && !hasParentDataChanged && !m_LastCheckProgressState.IsFollowUpCheckRequired)
            {
                return dependsOn;
            }

            // If a new request has come in make sure a follow up check is scheduled for next frame.
            // Most of the time our progress data will get invalidated by the cancel unwind job of the Task Driver but
            // this mitigates two edge cases and prevents the tree of Task Drivers from soft locking:
            //   - There is no data in the cancel stream and the unwind job doesn't get scheduled. Since it isn't
            //     scheduled the data doesn't get invalidated to trigger this work.
            //   - The developer developer hasn't configured a job to unwind tasks canceled from a stream configured to
            //     unwind.
            m_LastCheckProgressState.IsFollowUpCheckRequired =
                m_RequestLookupData.IsDataInvalidated(m_LastCheckProgressState.RequestLookupDataVersion);

            //TODO: #136 - Potentially have the Acquire grant access to the data within
            m_Dependencies[0] = dependsOn;
            m_Dependencies[1] = m_ProgressLookupData.AcquireAsync(AccessType.ExclusiveWrite);
            m_Dependencies[2] = m_CancelCompleteData.AcquireAsync(AccessType.SharedWrite);
            m_Dependencies[3] = m_Parent?.m_ProgressLookupData.AcquireAsync(AccessType.ExclusiveWrite) ?? default;

            dependsOn = JobHandle.CombineDependencies(m_Dependencies);

            CheckCancelProgressJob checkCancelProgressJob = new CheckCancelProgressJob(
                m_ProgressLookupData.Lookup,
                m_CancelCompleteData.PendingWriter,
                m_CancelCompleteDataTargetID,
                m_ProgressLookupData.DataOwner.WorldUniqueID,
                m_Parent?.m_ProgressLookupData.Lookup ?? default);

            dependsOn = checkCancelProgressJob.Schedule(dependsOn);

            m_ProgressLookupData.ReleaseAsync(dependsOn);
            m_CancelCompleteData.ReleaseAsync(dependsOn);
            m_Parent?.m_ProgressLookupData.ReleaseAsync(dependsOn);

            m_LastCheckProgressState.RequestLookupDataVersion = m_RequestLookupData.Version;
            m_LastCheckProgressState.ProgressLookupDataVersion = m_ProgressLookupData.Version;

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
            private UnsafeParallelHashMap<EntityKeyedTaskID, bool> m_ProgressLookup;
            [ReadOnly] private readonly UnsafeTypedStream<EntityKeyedTaskWrapper<CancelComplete>>.Writer m_CompleteWriter;
            private readonly DataTargetID m_CancelCompleteDataTargetID;
            private UnsafeParallelHashMap<EntityKeyedTaskID, bool> m_ParentProgressLookup;
            private readonly DataOwnerID m_DataOwnerID;

            private UnsafeTypedStream<EntityKeyedTaskWrapper<CancelComplete>>.LaneWriter m_CompleteLaneWriter;

            public CheckCancelProgressJob(
                UnsafeParallelHashMap<EntityKeyedTaskID, bool> progressLookup,
                UnsafeTypedStream<EntityKeyedTaskWrapper<CancelComplete>>.Writer completeWriter,
                DataTargetID cancelCompleteDataTargetID,
                DataOwnerID dataOwnerID,
                UnsafeParallelHashMap<EntityKeyedTaskID, bool> parentProgressLookup)
            {
                m_ProgressLookup = progressLookup;
                m_CompleteWriter = completeWriter;
                m_CancelCompleteDataTargetID = cancelCompleteDataTargetID;
                m_ParentProgressLookup = parentProgressLookup;
                m_DataOwnerID = dataOwnerID;

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
                m_CompleteLaneWriter = default;
            }

            public void Execute()
            {
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                m_CompleteLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);

                if (m_ParentProgressLookup.IsCreated)
                {
                    CheckCancelProgressWithParent();
                }
                else
                {
                    CheckCancelProgress();
                }
            }

            private void CheckCancelProgressWithParent()
            {
                //We need to loop through the parent collection because if we are a System, then there could be
                //multiple TaskDrivers that have requested a cancel. If we iterate through ourself, we're going
                //to accidentally mark things complete in different task drivers.
                foreach (KeyValue<EntityKeyedTaskID, bool> entry in m_ParentProgressLookup)
                {
                    EntityKeyedTaskID id = new EntityKeyedTaskID(entry.Key, m_DataOwnerID);

                    bool willComplete = CheckIfWillComplete(m_ProgressLookup[id], ref id);
                    if (!willComplete)
                    {
                        //If we aren't going to complete, then our parent needs to stay held open.
                        //If we are going to complete, then we won't assume anything and leave it alone.
                        //Our parent might have other children to wait on which will hold the parent open OR
                        //our parent might have nothing, in which case we don't want to hold open for an unnecessary
                        //extra frame.
                        // entry.Value == isParentProcessing
                        entry.Value = true;
                    }
                }
            }

            private void CheckCancelProgress()
            {
                //We don't have a parent so we must be the top level TaskDriver.
                //We need to loop through ourselves instead.
                foreach (KeyValue<EntityKeyedTaskID, bool> entry in m_ProgressLookup)
                {
                    EntityKeyedTaskID id = entry.Key;
                    CheckIfWillComplete(entry.Value, ref id);
                }
            }

            private bool CheckIfWillComplete(bool isStillProcessing, ref EntityKeyedTaskID id)
            {
                if (isStillProcessing)
                {
                    //Flip us back to not processing. A CancelJob will switch this if we still need to process
                    m_ProgressLookup[id] = false;
                    return false;
                }
                //If we're not processing then:
                // - All Cancel Jobs are complete
                // OR
                // - There never were any Cancel Jobs to begin with
                // OR
                // - There wasn't any data for this id that was requested to cancel.{
                else
                {
                    //Remove ourselves from the Progress Lookup
                    m_ProgressLookup.Remove(id);
                    //Write ourselves to the Complete.
                    CancelComplete cancelComplete = new CancelComplete(id.Entity);
                    m_CompleteLaneWriter.Write(
                        new EntityKeyedTaskWrapper<CancelComplete>(
                            id.Entity,
                            id.DataOwnerID,
                            m_CancelCompleteDataTargetID,
                            ref cancelComplete));

                    return true;
                }
            }
        }

        /// <summary>
        /// A wrapper that provides access to data version(s) shared between <see cref="ITaskSetOwner"/> instances.
        /// This allows multiple <see cref="CancelProgressFlowNode"/>s that point to the same <see cref="ITaskSetOwner"/>
        /// instance to track whether data for a given version has been processed.
        ///
        /// Call <see cref="Dispose()"/> on the instance when it is no longer required to release shared version
        /// references.
        /// </summary>
        /// <remarks>
        /// Example:
        /// Multiple <see cref="CancelProgressFlowNode"/>s point to each <see cref="TaskDriverSystem{TTaskDriverType}"/>
        /// instance.
        /// </remarks>
        private sealed class OwnerSharedCheckProgressState : IDisposable
        {
            private StateRefWrapper m_Ref;

            public uint ProgressLookupDataVersion
            {
                get => m_Ref.ProgressLookupDataVersion;
                set => m_Ref.ProgressLookupDataVersion = value;
            }

            public uint RequestLookupDataVersion
            {
                get => m_Ref.RequestDataVersion;
                set => m_Ref.RequestDataVersion = value;
            }

            /// <see cref="StateRefWrapper.IsFollowUpCheckRequired"/>>
            public bool IsFollowUpCheckRequired
            {
                get => m_Ref.IsFollowUpCheckRequired;
                set => m_Ref.IsFollowUpCheckRequired = value;
            }

            public OwnerSharedCheckProgressState(ITaskSetOwner owner)
            {
                m_Ref = StateRefWrapper.Acquire(owner);
            }

            public void Dispose()
            {
                Debug.Assert(m_Ref != null);
                m_Ref?.Release();
                m_Ref = null;
            }


            private sealed class StateRefWrapper
            {
                private static Dictionary<ITaskSetOwner, StateRefWrapper> s_ExistingInstanceLookup;

                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
                private static void Init()
                {
                    s_ExistingInstanceLookup = new Dictionary<ITaskSetOwner, StateRefWrapper>();
                }

                public static StateRefWrapper Acquire(ITaskSetOwner owner)
                {
                    if (!s_ExistingInstanceLookup.TryGetValue(owner, out StateRefWrapper instanceVersion))
                    {
                        instanceVersion = new StateRefWrapper(owner);
                        s_ExistingInstanceLookup.Add(owner, instanceVersion);
                    }

                    Debug.Assert(instanceVersion.m_RefCount < byte.MaxValue);
                    instanceVersion.m_RefCount++;

                    return instanceVersion;
                }

                public uint ProgressLookupDataVersion;
                public uint RequestDataVersion;

                /// <summary>
                /// If true, indicates that a follow up check is required on the next pass.
                /// Regardless of the state of data invalidation.
                /// </summary>
                public bool IsFollowUpCheckRequired;

                private readonly ITaskSetOwner m_Owner;
                private byte m_RefCount;


                private StateRefWrapper(ITaskSetOwner owner)
                {
                    m_Owner = owner;
                    ProgressLookupDataVersion = 0;
                    m_RefCount = 0;
                }

                public void Release()
                {
                    Debug.Assert(s_ExistingInstanceLookup.ContainsKey(m_Owner));
                    m_RefCount--;
                    if (m_RefCount <= 0)
                    {
                        s_ExistingInstanceLookup.Remove(m_Owner);
                    }
                }
            }
        }
    }
}