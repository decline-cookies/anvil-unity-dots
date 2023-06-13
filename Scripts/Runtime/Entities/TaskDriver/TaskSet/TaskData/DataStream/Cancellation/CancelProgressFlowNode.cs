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
                nameof(ScheduleCheckCancelProgressJob),
                BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly ITaskSetOwner m_TaskSetOwner;
        private readonly CancelProgressFlowNode m_Parent;
        private readonly ActiveLookupData<EntityProxyInstanceID> m_ProgressLookupData;
        private readonly PendingData<EntityProxyInstanceWrapper<CancelComplete>> m_CancelCompleteData;
        private readonly DataTargetID m_CancelCompleteDataTargetID;

        private OwnerSharedDataVersion m_LastProcessedDataVersion;
        private NativeArray<JobHandle> m_Dependencies;


        public CancelProgressFlowNode(ITaskSetOwner taskSetOwner, CancelProgressFlowNode parent)
        {
            m_TaskSetOwner = taskSetOwner;
            m_Parent = parent;

            m_ProgressLookupData = m_TaskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData;
            m_CancelCompleteData = m_TaskSetOwner.TaskSet.CancelCompleteDataStream.PendingData;
            m_CancelCompleteDataTargetID = m_TaskSetOwner.TaskSet.CancelCompleteDataStream.DataTargetID;

            m_LastProcessedDataVersion = new OwnerSharedDataVersion(m_TaskSetOwner);
            m_Dependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            m_LastProcessedDataVersion.Dispose();
            m_Dependencies.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{m_TaskSetOwner} - With CancelRequestData of {m_TaskSetOwner.TaskSet.CancelRequestsDataStream.DataTargetID} and CancelProgressData of {m_TaskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData.WorldUniqueID} and CancelCompleteData of {m_TaskSetOwner.TaskSet.CancelCompleteDataStream.DataTargetID}";
        }

        private bool IsProgressLookupDataInvalidated()
        {
            return m_ProgressLookupData.IsDataInvalidated(m_LastProcessedDataVersion.ProgressLookupDataVersion);
        }

        private JobHandle ScheduleCheckCancelProgressJob(JobHandle dependsOn)
        {
            bool hasOurDataChanged = IsProgressLookupDataInvalidated();
            bool hasParentDataChanged = m_Parent?.IsProgressLookupDataInvalidated() ?? false;

            //If nothing changed, we don't need to schedule
            if (!hasOurDataChanged && !hasParentDataChanged)
            {
                return dependsOn;
            }

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

            m_LastProcessedDataVersion.ProgressLookupDataVersion = m_ProgressLookupData.Version;

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
            [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceWrapper<CancelComplete>>.Writer m_CompleteWriter;
            private readonly DataTargetID m_CancelCompleteDataTargetID;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ParentProgressLookup;
            private readonly DataOwnerID m_DataOwnerID;

            private UnsafeTypedStream<EntityProxyInstanceWrapper<CancelComplete>>.LaneWriter m_CompleteLaneWriter;

            public CheckCancelProgressJob(
                UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup,
                UnsafeTypedStream<EntityProxyInstanceWrapper<CancelComplete>>.Writer completeWriter,
                DataTargetID cancelCompleteDataTargetID,
                DataOwnerID dataOwnerID,
                UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup)
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
                foreach (KeyValue<EntityProxyInstanceID, bool> entry in m_ParentProgressLookup)
                {
                    EntityProxyInstanceID id = new EntityProxyInstanceID(entry.Key, m_DataOwnerID);

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
                foreach (KeyValue<EntityProxyInstanceID, bool> entry in m_ProgressLookup)
                {
                    EntityProxyInstanceID id = entry.Key;
                    CheckIfWillComplete(entry.Value, ref id);
                }
            }

            private bool CheckIfWillComplete(bool isStillProcessing, ref EntityProxyInstanceID id)
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
                        new EntityProxyInstanceWrapper<CancelComplete>(
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
        private struct OwnerSharedDataVersion : IDisposable
        {
            private VersionRefWrapper m_Ref;

            public uint ProgressLookupDataVersion
            {
                get => m_Ref.ProgressLookupDataVersion;
                set => m_Ref.ProgressLookupDataVersion = value;
            }


            public OwnerSharedDataVersion(ITaskSetOwner owner)
            {
                m_Ref = VersionRefWrapper.Acquire(owner);
            }

            public void Dispose()
            {
                m_Ref.Release();
                m_Ref = null;
            }


            private class VersionRefWrapper
            {
                private static Dictionary<ITaskSetOwner, VersionRefWrapper> s_ExistingInstanceLookup;

                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
                private static void Init()
                {
                    s_ExistingInstanceLookup = new Dictionary<ITaskSetOwner, VersionRefWrapper>();
                }

                public static VersionRefWrapper Acquire(ITaskSetOwner owner)
                {
                    if (!s_ExistingInstanceLookup.TryGetValue(owner, out VersionRefWrapper instanceVersion))
                    {
                        instanceVersion = new VersionRefWrapper(owner);
                        s_ExistingInstanceLookup.Add(owner, instanceVersion);
                    }

                    Debug.Assert(instanceVersion.m_RefCount < byte.MaxValue);
                    instanceVersion.m_RefCount++;

                    return instanceVersion;
                }

                public uint ProgressLookupDataVersion;
                private readonly ITaskSetOwner m_Owner;
                private byte m_RefCount;


                private VersionRefWrapper(ITaskSetOwner owner)
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