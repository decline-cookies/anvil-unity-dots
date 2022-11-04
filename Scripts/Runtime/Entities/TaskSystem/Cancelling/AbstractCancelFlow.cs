using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractCancelFlow : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractCancelFlow> CHECK_PROGRESS_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractCancelFlow>(nameof(ScheduleCheckCancelProgressJob), BindingFlags.Instance | BindingFlags.NonPublic);
        
        //Complete data
        private NativeArray<JobHandle> m_Dependencies;

        protected CancelData CancelData { get; }
        protected TaskDriverCancelFlow Parent { get; }
        
        //TODO: Hide this stuff when collections checks are disabled
        protected ProfilerMarker Debug_ProfilerMarker { get; }
        protected FixedString128Bytes Debug_DebugString { get; }

        protected AbstractCancelFlow(CancelData cancelData, TaskDriverCancelFlow parent)
        {
            CancelData = cancelData;
            Parent = parent;
            m_Dependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);

            Debug_DebugString = new FixedString128Bytes($"CancelFlow, {TaskDebugUtil.GetLocationName(CancelData.CompleteDataStream.OwningTaskSystem, CancelData.CompleteDataStream.OwningTaskDriver)}");
            Debug_ProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, Debug_DebugString.Value, MarkerFlags.Script);
        }

        protected override void DisposeSelf()
        {
            m_Dependencies.Dispose();
            
            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

        private JobHandle ScheduleCheckCancelProgressJob(JobHandle dependsOn)
        {
            //TODO: THIS MIGHT NOT FULLY WORK
            if (Parent == null)
            {
                return dependsOn;
            }
            
            UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup = default;

            m_Dependencies[0] = dependsOn;
            m_Dependencies[1] = CancelData.AcquireProgressLookup(AccessType.ExclusiveWrite, out UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup);
            m_Dependencies[2] = CancelData.CompleteDataStream.AccessController.AcquireAsync(AccessType.SharedWrite);
            m_Dependencies[3] = Parent?.CancelData.AcquireProgressLookup(AccessType.ExclusiveWrite, out parentProgressLookup) ?? default;

            dependsOn = JobHandle.CombineDependencies(m_Dependencies);

            CheckCancelProgressJob checkCancelProgressJob = new CheckCancelProgressJob(parentProgressLookup,
                                                                                       progressLookup,
                                                                                       CancelData.CompleteDataStream.Pending,
                                                                                       Parent?.TaskDriverContext ?? 0,
                                                                                       Debug_DebugString,
                                                                                       Debug_ProfilerMarker);

            dependsOn = checkCancelProgressJob.Schedule(dependsOn);

            CancelData.ReleaseProgressLookup(dependsOn);
            CancelData.CompleteDataStream.AccessController.ReleaseAsync(dependsOn);
            Parent?.CancelData.ReleaseProgressLookup(dependsOn);

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
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceID> m_CompleteWriter;
            [ReadOnly] private readonly byte m_Context;
            
            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;

            public CheckCancelProgressJob(UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup,
                                          UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup,
                                          UnsafeTypedStream<EntityProxyInstanceID> completeWriter,
                                          byte context,
                                          FixedString128Bytes debugString,
                                          ProfilerMarker profilerMarker) : this()
            {
                m_ParentProgressLookup = parentProgressLookup;
                m_ProgressLookup = progressLookup;
                m_CompleteWriter = completeWriter;
                m_Context = context;

                m_DebugString = debugString;
                m_ProfilerMarker = profilerMarker;

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter completeLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);

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

                    //If we're still processing...
                    if (m_ProgressLookup[id] == true)
                    {
                        Debug.Log($"Still processing for {id.ToFixedString()} on {m_DebugString} - Holding open parent");
                        //Flip us back to not processing. A CancelJob will switch this if we still need to process
                        m_ProgressLookup[id] = false;
                        //Hold open the parent, the parent shouldn't collapse until nothing is holding it open
                        isParentProcessing = true;
                    }
                    //If we're not processing then:
                    // - All Cancel Jobs are complete 
                    // OR
                    // - There never were any Cancel Jobs to begin with
                    // OR
                    // - There wasn't any data for this id that was requested to cancel.
                    else
                    {
                        Debug.Log($"No longer processing for {id.ToFixedString()} on {m_DebugString} - Completing");
                        //Remove ourselves from the Progress Lookup
                        m_ProgressLookup.Remove(id);
                        //Write ourselves to the Complete.
                        completeLaneWriter.Write(id);
                    }
                }
                
                m_ProfilerMarker.End();
            }
        }
    }
}
