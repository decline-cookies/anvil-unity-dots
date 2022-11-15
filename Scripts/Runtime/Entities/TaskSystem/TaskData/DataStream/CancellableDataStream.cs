using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if DEBUG
using Unity.Profiling;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
using UnityEngine;
#endif

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancellableDataStream<TInstance> : DataStream<TInstance>,
                                                      ICancellableDataStream<TInstance>,
                                                      IUntypedCancellableDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private NativeArray<JobHandle> m_ConsolidationDependencies;

        public PendingCancelDataStream<TInstance> PendingCancelDataStream { get; }

        public AbstractDataStream UntypedPendingCancelDataStream
        {
            get => PendingCancelDataStream;
        }

        public CancellableDataStream(CancelRequestDataStream taskDriverCancelRequests, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriverCancelRequests, taskDriver, taskSystem)
        {
            PendingCancelDataStream = new PendingCancelDataStream<TInstance>(taskDriver, taskSystem);
            m_ConsolidationDependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);
        }

        protected override void DisposeDataStream()
        {
            m_ConsolidationDependencies.Dispose();
            PendingCancelDataStream.Dispose();
            base.DisposeDataStream();
        }
        
        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        protected sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            m_ConsolidationDependencies[0] = dependsOn;
            m_ConsolidationDependencies[1] = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            m_ConsolidationDependencies[2] = PendingCancelDataStream.AccessController.AcquireAsync(AccessType.SharedWrite);
            m_ConsolidationDependencies[3] = TaskDriverCancelRequests.AccessController.AcquireAsync(AccessType.SharedRead);

            ConsolidateCancellableDataStreamJob consolidateJob = new ConsolidateCancellableDataStreamJob(Pending,
                                                                                                         Live,
                                                                                                         PendingCancelDataStream.Pending.AsWriter(),
                                                                                                         TaskDriverCancelRequests.Lookup
#if DEBUG
                                                                                                        ,
                                                                                                         Debug_ProfilingInfo.ProfilingDetails
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                                                                                        ,
                                                                                                         Debug_DebugString
#endif
                                                                                                        );


            dependsOn = consolidateJob.Schedule(JobHandle.CombineDependencies(m_ConsolidationDependencies));

            AccessController.ReleaseAsync(dependsOn);
            PendingCancelDataStream.AccessController.ReleaseAsync(dependsOn);
            TaskDriverCancelRequests.AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        //TODO: Could do a two pass ideal where we consolidate fast, then iterate to find instances that are cancelled, then consolidate fast again. Not sure how useful that is
        [BurstCompile]
        private struct ConsolidateCancellableDataStreamJob : IJob
        {
            private const int UNSET_THREAD_INDEX = -1;

            [NativeSetThreadIndex] private readonly int m_NativeThreadIndex;
            [ReadOnly] private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_CancelRequestsForID;
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Live;
            [WriteOnly] private readonly UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer m_PendingCancelledWriter;

#if DEBUG
            private DataStreamProfilingDetails m_ProfilingDetails;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
#endif


            public ConsolidateCancellableDataStreamJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                                       DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> live,
                                                       UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer pendingCancelledWriter,
                                                       UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelRequests
#if DEBUG
                                                      ,
                                                       DataStreamProfilingDetails profilingDetails
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                                      ,
                                                       FixedString128Bytes debugString
#endif
            ) : this()
            {
                m_Pending = pending;
                m_Live = live;
                m_PendingCancelledWriter = pendingCancelledWriter;
                m_CancelRequestsForID = cancelRequests;

#if DEBUG
                m_ProfilingDetails = profilingDetails;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                m_DebugString = debugString;
#endif

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
            }

            public void Execute()
            {
#if DEBUG
                m_ProfilingDetails.ProfilerMarker.Begin();
#endif
                m_Live.Clear();

                UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.LaneWriter pendingCancelledLaneWriter = m_PendingCancelledWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex));

                int pendingCount = m_Pending.Count();
                NativeArray<EntityProxyInstanceWrapper<TInstance>> iterationArray = m_Live.DeferredCreate(pendingCount);

                int liveIndex = 0;
                foreach (EntityProxyInstanceWrapper<TInstance> instance in m_Pending)
                {
                    if (m_CancelRequestsForID.ContainsKey(instance.InstanceID))
                    {
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                        Debug.Log($"Cancelling Instance with ID {instance.InstanceID.ToFixedString()} for {m_DebugString}");
#endif
                        pendingCancelledLaneWriter.Write(instance);
                    }
                    else
                    {
                        iterationArray[liveIndex] = instance;
                        liveIndex++;
                    }
                }

                m_Live.ResetLengthTo(liveIndex);
                m_Pending.Clear();

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                if (liveIndex > 0)
                {
                    Debug.Log($"{m_DebugString} - Count: {liveIndex}");
                }
#endif
#if DEBUG
                m_ProfilingDetails.PendingCapacity = m_Pending.Capacity();
                m_ProfilingDetails.LiveInstances = m_Live.Length;
                m_ProfilingDetails.LiveCapacity = m_Live.Capacity;
                m_ProfilingDetails.ProfilerMarker.End();
#endif
            }
        }
    }
}
