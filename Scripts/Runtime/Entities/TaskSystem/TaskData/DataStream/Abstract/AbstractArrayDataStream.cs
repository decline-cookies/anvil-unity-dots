using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
#if DEBUG
using Unity.Profiling;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
using System;
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


            ConsolidateArrayDataStreamJob consolidateArrayDataStreamJob = new ConsolidateArrayDataStreamJob(Pending,
                                                                                                            Live
#if DEBUG
                                                                                                           ,
                                                                                                            Debug_ProfilerMarker
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                                                                                           ,
                                                                                                            Debug_DebugString
#endif
                                                                                                           );
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

#if DEBUG
            private readonly ProfilerMarker m_ProfilerMarker;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
#endif


            public ConsolidateArrayDataStreamJob(UnsafeTypedStream<T> pending,
                                                 DeferredNativeArray<T> live
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
                m_Pending = pending;
                m_Live = live;

#if DEBUG
                m_ProfilerMarker = profilerMarker;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                m_DebugString = debugString;
#endif
            }


            public void Execute()
            {
#if DEBUG
                m_ProfilerMarker.Begin();
#endif
                m_Live.Clear();

                int pendingCount = m_Pending.Count();
                NativeArray<T> liveArray = m_Live.DeferredCreate(pendingCount);
                m_Pending.CopyTo(ref liveArray);
                m_Pending.Clear();

                //TODO: Custom profiler module
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                if (iterationArray.Length > 0)
                {
                    Debug.Log($"{m_DebugString} - Count {iterationArray.Length}");
                }
#endif
#if DEBUG
                m_ProfilerMarker.End();
#endif
            }
        }
    }
}
