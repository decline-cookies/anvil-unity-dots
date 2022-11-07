using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
using Unity.Profiling;
using UnityEngine;
#endif

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStream<TInstance> : AbstractArrayDataStream<EntityProxyInstanceWrapper<TInstance>>,
                                           IDataStream<TInstance>,
                                           IInternalDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //TODO: Move to Util
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceWrapper<TInstance>>();

        public CancelRequestDataStream TaskDriverCancelRequests { get; }


        public DataStream(CancelRequestDataStream taskDriverCancelRequests, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            //We don't own the m_CancelRequestsDataStream so we don't dispose it.
            TaskDriverCancelRequests = taskDriverCancelRequests;
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************
        public DataStreamReader<TInstance> CreateDataStreamReader()
        {
            return new DataStreamReader<TInstance>(Live.AsDeferredJobArray());
        }

        public DataStreamUpdater<TInstance> CreateDataStreamUpdater(DataStreamTargetResolver dataStreamTargetResolver)
        {
            return new DataStreamUpdater<TInstance>(Pending.AsWriter(),
                                                    Live.AsDeferredJobArray(),
                                                    dataStreamTargetResolver);
        }

        public DataStreamWriter<TInstance> CreateDataStreamWriter(byte context)
        {
            return new DataStreamWriter<TInstance>(Pending.AsWriter(), context);
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        protected override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            //TODO: If anything was cancelled, we should write to the Cancel complete because we've already said we don't care
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      TaskDriverCancelRequests.AccessController.AcquireAsync(AccessType.SharedRead),
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite));

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            ConsolidateDataStreamJob consolidateJob = new ConsolidateDataStreamJob(Pending,
                                                                                   Live,
                                                                                   TaskDriverCancelRequests.Lookup,
                                                                                   Debug_DebugString,
                                                                                   Debug_ProfilerMarker);
#else
            ConsolidateDataStreamJob consolidateJob = new ConsolidateDataStreamJob(Pending,
                                                                                   Live,
                                                                                   TaskDriverCancelRequests.Lookup);
#endif
            dependsOn = consolidateJob.Schedule(dependsOn);

            AccessController.ReleaseAsync(dependsOn);
            TaskDriverCancelRequests.AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateDataStreamJob : IJob
        {
            [ReadOnly] private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_CancelRequests;
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Live;

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;

            public ConsolidateDataStreamJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> live,
                                            UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelRequests,
                                            FixedString128Bytes debugString,
                                            ProfilerMarker profilerMarker) : this()
            {
                m_Pending = pending;
                m_Live = live;
                m_CancelRequests = cancelRequests;
                m_DebugString = debugString;
                m_ProfilerMarker = profilerMarker;
            }
#else
            public ConsolidateDataStreamJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> live,
                                            UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelRequests) : this()
            {
                m_Pending = pending;
                m_Live = live;
                m_CancelRequests = cancelRequests;
            }
#endif


            public void Execute()
            {
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
                m_ProfilerMarker.Begin();
#endif
                m_Live.Clear();

                int pendingCount = m_Pending.Count();
                NativeArray<EntityProxyInstanceWrapper<TInstance>> liveArray = m_Live.DeferredCreate(pendingCount);

                int liveIndex = 0;
                foreach (EntityProxyInstanceWrapper<TInstance> instance in m_Pending)
                {
                    if (!m_CancelRequests.ContainsKey(instance.InstanceID))
                    {
                        liveArray[liveIndex] = instance;
                        liveIndex++;
                    }
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
                    else
                    {
                        Debug.Log($"Cancelling Instance with ID {instance.InstanceID.ToFixedString()} for {m_DebugString}");
                    }
#endif
                }

                //TODO: Make sure this makes sense
                m_Live.ResetLengthTo(liveIndex);
                m_Pending.Clear();

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
                if (liveIndex > 0)
                {
                    Debug.Log($"{m_DebugString} - Count: {liveIndex}");
                }
                
                m_ProfilerMarker.End();
#endif
            }
        }
    }
}
