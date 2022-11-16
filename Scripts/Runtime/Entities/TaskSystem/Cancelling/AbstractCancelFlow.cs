using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if DEBUG
using Unity.Profiling;
using Unity.Profiling.LowLevel;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
using UnityEngine;
#endif

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractCancelFlow : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractCancelFlow> CHECK_PROGRESS_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractCancelFlow>(nameof(ScheduleCheckCancelProgressJob), BindingFlags.Instance | BindingFlags.NonPublic);


        private NativeArray<JobHandle> m_Dependencies;

        protected TaskData TaskData { get; }
        protected TaskDriverCancelFlow Parent { get; }

#if DEBUG
        private ProfilerMarker Debug_ProfilerMarker { get; }
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
        private FixedString128Bytes Debug_DebugString { get; }
#endif

        protected AbstractCancelFlow(TaskData taskData, TaskDriverCancelFlow parent)
        {
            TaskData = taskData;
            Parent = parent;
            m_Dependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);

#if DEBUG
            Debug_ProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, ToString(), MarkerFlags.Script);
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            Debug_DebugString = new FixedString128Bytes(ToString());
#endif
        }

        protected override void DisposeSelf()
        {
            m_Dependencies.Dispose();

            base.DisposeSelf();
        }

        public sealed override string ToString()
        {
            return $"{GetType().GetReadableName()}, {TaskDebugUtil.GetLocationName(TaskData.TaskSystem, TaskData.TaskDriver)}";
        }

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

        private JobHandle ScheduleCheckCancelProgressJob(JobHandle dependsOn)
        {
            UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup = default;

            m_Dependencies[0] = dependsOn;
            m_Dependencies[1] = TaskData.CancelProgressLookup.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup);
            m_Dependencies[2] = TaskData.CancelCompleteDataStream.AccessController.AcquireAsync(AccessType.SharedWrite);
            m_Dependencies[3] = Parent?.TaskData.CancelProgressLookup.AcquireAsync(AccessType.ExclusiveWrite, out parentProgressLookup) ?? default;

            dependsOn = JobHandle.CombineDependencies(m_Dependencies);

            CheckCancelProgressJob checkCancelProgressJob = new CheckCancelProgressJob(parentProgressLookup,
                                                                                       progressLookup,
                                                                                       TaskData.CancelCompleteDataStream.Pending.AsWriter(),
                                                                                       Parent?.TaskDriverContext ?? 0
#if DEBUG
                                                                                      ,
                                                                                       Debug_ProfilerMarker
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                                                                      ,
                                                                                       Debug_DebugString
#endif
                                                                                      );

            dependsOn = checkCancelProgressJob.Schedule(dependsOn);

            TaskData.CancelProgressLookup.ReleaseAsync(dependsOn);
            TaskData.CancelCompleteDataStream.AccessController.ReleaseAsync(dependsOn);
            Parent?.TaskData.CancelProgressLookup.ReleaseAsync(dependsOn);

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

            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ParentProgressLookup;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
            [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CompleteWriter;
            [ReadOnly] private readonly byte m_Context;

            private UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter m_CompleteLaneWriter;

#if DEBUG
            private readonly ProfilerMarker m_ProfilerMarker;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
#endif

            public CheckCancelProgressJob(UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup,
                                          UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup,
                                          UnsafeTypedStream<EntityProxyInstanceID>.Writer completeWriter,
                                          byte context
#if DEBUG
                                         ,
                                          ProfilerMarker profilerMarker
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                         ,
                                          FixedString128Bytes debugString
#endif
            ) : this()

            {
                m_ParentProgressLookup = parentProgressLookup;
                m_ProgressLookup = progressLookup;
                m_CompleteWriter = completeWriter;
                m_Context = context;

#if DEBUG
                m_ProfilerMarker = profilerMarker;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                m_DebugString = debugString;
#endif

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
            }

            public void Execute()
            {
#if DEBUG
                m_ProfilerMarker.Begin();
#endif

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

#if DEBUG
                m_ProfilerMarker.End();
#endif
            }

            private void CheckCancelProgressWithParent()
            {
                //Go through all entries in the parent lookup. 
                //If we are a System, our parent must be a TaskDriver.
                //Therefore we could have have entries from multiple TaskDrivers but we only want to process 
                //one in this job for context and completion bubble up purposes
                //If we are a TaskDriver, then our parent must also still be a TaskDriver and the logic holds.
                foreach (KeyValue<EntityProxyInstanceID, bool> entry in m_ParentProgressLookup)
                {
                    EntityProxyInstanceID parentID = entry.Key;
                    ref bool isParentProcessing = ref entry.Value;
                    EntityProxyInstanceID id = new EntityProxyInstanceID(parentID.Entity, m_Context);
                    
                    bool isStillProcessing = m_ProgressLookup[id];
                    HandleProgress(isStillProcessing, ref id);
                    if (isParentProcessing)
                    {
                        //Hold open the parent, the parent shouldn't collapse until nothing is holding it open
                        isParentProcessing = true;
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
                    HandleProgress(entry.Value, ref id);
                }
            }

            private void HandleProgress(bool isStillProcessing, ref EntityProxyInstanceID id)
            {
                //If we're still processing...
                if (isStillProcessing)
                {
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                    Debug.Log($"Still processing for {id.ToFixedString()} on {m_DebugString} - Holding open");
#endif
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
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                    Debug.Log($"No longer processing for {id.ToFixedString()} on {m_DebugString} - Completing");
#endif
                    //Remove ourselves from the Progress Lookup
                    m_ProgressLookup.Remove(id);
                    //Write ourselves to the Complete.
                    m_CompleteLaneWriter.Write(id);
                }
            }
        }
    }
}
