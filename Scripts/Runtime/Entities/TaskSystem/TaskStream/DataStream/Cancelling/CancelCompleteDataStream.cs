using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteDataStream : AbstractEntityInstanceIDDataStream
    {
        internal DeferredNativeArray<EntityProxyInstanceID> Live { get; }

        internal CancelCompleteDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            Live = new DeferredNativeArray<EntityProxyInstanceID>(Allocator.Persistent,
                                                                  Allocator.TempJob);
        }

        protected override void DisposeDataStream()
        {
            Live.Dispose();
            base.DisposeDataStream();
        }

        protected override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite));

            ConsolidateCancelCompleteJob consolidateCancelCompleteJob = new ConsolidateCancelCompleteJob(Pending, 
                                                                                                         Live,
                                                                                                         Debug_DebugString,
                                                                                                         Debug_ProfilerMarker);
            dependsOn = consolidateCancelCompleteJob.Schedule(dependsOn);

            AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelCompleteJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceID> m_Iteration;

            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;

            public ConsolidateCancelCompleteJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                DeferredNativeArray<EntityProxyInstanceID> iteration,
                                                FixedString128Bytes debugString,
                                                ProfilerMarker profilerMarker)
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_DebugString = debugString;
                m_ProfilerMarker = profilerMarker;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                
                m_Iteration.Clear();

                int pendingCount = m_Pending.Count();
                NativeArray<EntityProxyInstanceID> iterationArray = m_Iteration.DeferredCreate(pendingCount);
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();

                if (iterationArray.Length > 0)
                {
                    Debug.Log($"{m_DebugString} - Length {iterationArray.Length}");
                }
                
                m_ProfilerMarker.End();
            }
        }
    }
}
