using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
using System;
using Unity.Profiling;
using UnityEngine;
#endif

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractArrayDataStream<T> : AbstractTypedDataStream<T>,
                                                         IArrayDataStream
        where T : unmanaged
    {
        public DeferredNativeArray<T> Live;
        
        public DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => Live.ScheduleInfo;
        }

        internal AbstractArrayDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            Live = new DeferredNativeArray<T>(Allocator.Persistent,
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

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            ConsolidateDataStreamBaseJob consolidateDataStreamBaseJob = new ConsolidateDataStreamBaseJob(Pending,
                                                                                                      Live,
                                                                                                      Debug_DebugString,
                                                                                                      Debug_ProfilerMarker);
#endif
            ConsolidateArrayDataStreamJob consolidateArrayDataStreamJob = new ConsolidateArrayDataStreamJob(Pending,
                                                                                                            Live);
            dependsOn = consolidateArrayDataStreamJob.Schedule(dependsOn);

            AccessController.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateArrayDataStreamJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<T> m_Pending;
            [WriteOnly] private DeferredNativeArray<T> m_Live;

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;
            
            public ConsolidateIgnoredCancelJob(UnsafeTypedStream<T> pending,
                                               DeferredNativeArray<T> iteration,
                                               FixedString128Bytes debugString,
                                               ProfilerMarker profilerMarker) : this()
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_DebugString = debugString;
                m_ProfilerMarker = profilerMarker;
            }
#endif

            public ConsolidateArrayDataStreamJob(UnsafeTypedStream<T> pending,
                                                 DeferredNativeArray<T> live) : this()
            {
                m_Pending = pending;
                m_Live = live;
            }


            public void Execute()
            {
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
                m_ProfilerMarker.Begin();
#endif
                m_Live.Clear();

                int pendingCount = m_Pending.Count();
                NativeArray<T> liveArray = m_Live.DeferredCreate(pendingCount);
                m_Pending.CopyTo(ref liveArray);
                m_Pending.Clear();

                //TODO: Custom profiler module
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
                if (iterationArray.Length > 0)
                {
                    Debug.Log($"{m_DebugString} - Count {iterationArray.Length}");
                }

                m_ProfilerMarker.End();
#endif
            }
        }
    }
}
