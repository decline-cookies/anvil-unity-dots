using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: Check all of these to see what is exposed on the outside
    public class DataStream<TInstance> : AbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceWrapper<TInstance>>();
        
        internal CancelRequestDataStream TaskDriverCancelRequests { get; }

        //We need this constructor for Reflection to create this instance the same way it creates the Cancellable Instance
        // ReSharper disable once RedundantOverload.Global
        // ReSharper disable once IntroduceOptionalParameters.Global
        internal DataStream(CancelRequestDataStream taskDriverCancelRequests, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : this(taskDriverCancelRequests, false, taskDriver, taskSystem)
        {
        }
        
        internal DataStream(CancelRequestDataStream taskDriverCancelRequests, bool isCancellable, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(isCancellable, taskDriver, taskSystem)
        {
            //We don't own the m_CancelRequestsDataStream so we don't dispose it.
            TaskDriverCancelRequests = taskDriverCancelRequests;
        }

        internal override AbstractDataStream GetCancelPendingDataStream()
        {
            throw new NotImplementedException("Doesn't support cancel!");
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************
        internal DataStreamReader<TInstance> CreateDataStreamReader()
        {
            return new DataStreamReader<TInstance>(Live.AsDeferredJobArray());
        }

        internal DataStreamUpdater<TInstance> CreateDataStreamUpdater(DataStreamTargetResolver dataStreamTargetResolver)
        {
            return new DataStreamUpdater<TInstance>(Pending.AsWriter(),
                                                    Live.AsDeferredJobArray(),
                                                    dataStreamTargetResolver);
        }

        internal DataStreamWriter<TInstance> CreateDataStreamWriter(byte context)
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
                                                                                   TaskDriverCancelRequests.Lookup,
                                                                                   Debug_DebugString,
                                                                                   Debug_ProfilerMarker);
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
            [ReadOnly] private UnsafeParallelHashMap<EntityProxyInstanceID, byte> m_CancelRequests;
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Live;
            
            
            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;

            public ConsolidateDataStreamJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                            DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> live,
                                            UnsafeParallelHashMap<EntityProxyInstanceID, byte> cancelRequests,
                                            FixedString128Bytes debugString,
                                            ProfilerMarker profilerMarker) : this()
            {
                m_Pending = pending;
                m_Live = live;
                m_CancelRequests = cancelRequests;
                m_DebugString = debugString;
                m_ProfilerMarker = profilerMarker;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                m_Live.Clear();

                int pendingCount = m_Pending.Count();
                NativeArray<EntityProxyInstanceWrapper<TInstance>> liveArray = m_Live.DeferredCreate(pendingCount);

                int liveIndex = 0;
                foreach (EntityProxyInstanceWrapper<TInstance> instance in m_Pending)
                {
                    if (m_CancelRequests.ContainsKey(instance.InstanceID))
                    {
                        Debug.Log($"Cancelling Instance with ID {instance.InstanceID} for {m_DebugString}");
                    }
                    else
                    {
                        liveArray[liveIndex] = instance;
                        liveIndex++;
                    }
                }
                
                //TODO: Make sure this makes sense
                m_Live.ResetLengthTo(liveIndex);

                Debug.Log($"{m_DebugString} - Count: {liveIndex}");

                m_Pending.Clear();

                m_ProfilerMarker.End();
            }
        }
    }
}
