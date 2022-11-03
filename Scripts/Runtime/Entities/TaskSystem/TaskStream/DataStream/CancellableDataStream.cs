using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class CancellableDataStream<TInstance> : DataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        internal CancelPendingDataStream<TInstance> CancelPendingDataStream { get; }

        private NativeArray<JobHandle> m_ConsolidationDependencies;
        
        internal CancellableDataStream(CancelRequestDataStream taskDriverCancelRequests, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriverCancelRequests, true, taskDriver, taskSystem)
        {
            CancelPendingDataStream = new CancelPendingDataStream<TInstance>(taskDriver, taskSystem);
            m_ConsolidationDependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);
        }

        protected override void DisposeDataStream()
        {
            m_ConsolidationDependencies.Dispose();
            CancelPendingDataStream.Dispose();
            base.DisposeDataStream();
        }
        
        internal override AbstractDataStream GetCancelPendingDataStream()
        {
            return CancelPendingDataStream;
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        protected sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            m_ConsolidationDependencies[0] = dependsOn;
            m_ConsolidationDependencies[1] = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            m_ConsolidationDependencies[2] = CancelPendingDataStream.AccessController.AcquireAsync(AccessType.SharedWrite);
            m_ConsolidationDependencies[3] = TaskDriverCancelRequests.AccessController.AcquireAsync(AccessType.SharedRead);

            ConsolidateCancellableDataStreamJob consolidateJob = new ConsolidateCancellableDataStreamJob(Pending,
                                                                                                         Live,
                                                                                                         CancelPendingDataStream.Pending.AsWriter(),
                                                                                                         TaskDriverCancelRequests.Lookup,
                                                                                                         Debug_DebugString,
                                                                                                         Debug_ProfilerMarker);
            dependsOn = consolidateJob.Schedule(JobHandle.CombineDependencies(m_ConsolidationDependencies));

            AccessController.ReleaseAsync(dependsOn);
            CancelPendingDataStream.AccessController.ReleaseAsync(dependsOn);
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
            [ReadOnly] private UnsafeParallelHashMap<EntityProxyInstanceID, byte> m_CancelRequestsForID;
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Iteration;
            [WriteOnly] private readonly UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer m_PendingCancelledWriter;
            
            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;

            public ConsolidateCancellableDataStreamJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                                       DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> iteration,
                                                       UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer pendingCancelledWriter,
                                                       UnsafeParallelHashMap<EntityProxyInstanceID, byte> cancelRequests,
                                                       FixedString128Bytes debugString,
                                                       ProfilerMarker profilerMarker) : this()
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_PendingCancelledWriter = pendingCancelledWriter;
                m_CancelRequestsForID = cancelRequests;
                m_DebugString = debugString;
                m_ProfilerMarker = profilerMarker;

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                m_Iteration.Clear();

                UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.LaneWriter pendingCancelledLaneWriter = m_PendingCancelledWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex));

                int pendingCount = m_Pending.Count();
                NativeArray<EntityProxyInstanceWrapper<TInstance>> iterationArray = m_Iteration.DeferredCreate(pendingCount);

                int liveIndex = 0;
                foreach (EntityProxyInstanceWrapper<TInstance> instance in m_Pending)
                {
                    if (m_CancelRequestsForID.ContainsKey(instance.InstanceID))
                    {
                        Debug.Log($"Cancelling Instance with ID {instance.InstanceID} for {m_DebugString}");
                        pendingCancelledLaneWriter.Write(instance);
                    }
                    else
                    {
                        iterationArray[liveIndex] = instance;
                        liveIndex++;
                    }
                }
                
                m_Iteration.ResetLengthTo(liveIndex);
                
                Debug.Log($"{m_DebugString} - Count: {liveIndex}");

                m_Pending.Clear();

                m_ProfilerMarker.End();
            }
        }
    }
}
