using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
#if DEBUG
using Unity.Profiling;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
using UnityEngine;
#endif

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractArrayDataStream<T> : AbstractTypedDataStream<T>,
                                                         IArrayDataStream
        where T : unmanaged
    {
        //Deliberately not using pointers because that messes up what the safety handle pointer points to.
        protected DeferredNativeArray<T> Live;

        public DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => Live.ScheduleInfo;
        }
        
#if DEBUG
        protected internal sealed override unsafe long Debug_LiveBytesPerInstance
        {
            get => sizeof(T);
        }
#endif

        protected AbstractArrayDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            Live = new DeferredNativeArray<T>(Allocator.Persistent,
                                              Allocator.TempJob);
        }

        protected override void DisposeDataStream()
        {
            Live.Dispose();
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
        
        protected override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite));


            ConsolidateArrayDataStreamJob consolidateArrayDataStreamJob = new ConsolidateArrayDataStreamJob(Pending,
                                                                                                            Live
#if DEBUG
                                                                                                           ,
                                                                                                            Debug_ProfilingStats.ProfilingInfo
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
            private ProfilingInfo m_ProfilingInfo;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
#endif


            public ConsolidateArrayDataStreamJob(UnsafeTypedStream<T> pending,
                                                 DeferredNativeArray<T> live
#if DEBUG
                                                ,
                                                 ProfilingInfo profilingInfo
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
                m_ProfilingInfo = profilingInfo;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                m_DebugString = debugString;
#endif
            }


            public void Execute()
            {
#if DEBUG
                m_ProfilingInfo.ProfilerMarker.Begin();
#endif
                m_Live.Clear();

                int pendingCount = m_Pending.Count();
                NativeArray<T> liveArray = m_Live.DeferredCreate(pendingCount);
                m_Pending.CopyTo(ref liveArray);
                m_Pending.Clear();

                //TODO: #108 -  Custom profiler module
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                if (liveArray.Length > 0)
                {
                    Debug.Log($"{m_DebugString} - Count {liveArray.Length}");
                }
#endif
#if DEBUG
                m_ProfilingInfo.PendingCapacity = m_Pending.Capacity();
                m_ProfilingInfo.LiveInstances = m_Live.Length;
                m_ProfilingInfo.LiveCapacity = m_Live.Capacity;
                m_ProfilingInfo.ProfilerMarker.End();
#endif
            }
        }
    }
}
