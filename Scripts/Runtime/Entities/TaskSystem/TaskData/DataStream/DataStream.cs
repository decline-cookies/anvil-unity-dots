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

            ConsolidateDataStreamJob consolidateJob = new ConsolidateDataStreamJob(Pending,
                                                                                   Live,
                                                                                   TaskDriverCancelRequests.Lookup
#if DEBUG
                                                                                  ,
                                                                                   Debug_ProfilerMarker
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                                                       ,Debug_DebugString
#endif
                                                                                  );
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

#if DEBUG
            private readonly ProfilerMarker m_ProfilerMarker;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
#endif


            public ConsolidateDataStreamJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> live,
                                            UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelRequests
#if DEBUG
                                           ,
                                            ProfilerMarker profilerMarker
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                   ,FixedString128Bytes debugString
#endif
            ) : this()
            {
                m_Pending = pending;
                m_Live = live;
                m_CancelRequests = cancelRequests;
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
                NativeArray<EntityProxyInstanceWrapper<TInstance>> liveArray = m_Live.DeferredCreate(pendingCount);

                int liveIndex = 0;
                foreach (EntityProxyInstanceWrapper<TInstance> instance in m_Pending)
                {
                    if (!m_CancelRequests.ContainsKey(instance.InstanceID))
                    {
                        liveArray[liveIndex] = instance;
                        liveIndex++;
                    }
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                    else
                    {
                        Debug.Log($"Cancelling Instance with ID {instance.InstanceID.ToFixedString()} for {m_DebugString}");
                    }
#endif
                }

                //TODO: Make sure this makes sense
                m_Live.ResetLengthTo(liveIndex);
                m_Pending.Clear();

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                if (liveIndex > 0)
                {
                    Debug.Log($"{m_DebugString} - Count: {liveIndex}");
                }
#endif
#if DEBUG
                m_ProfilerMarker.End();
#endif
            }
        }
    }
}
