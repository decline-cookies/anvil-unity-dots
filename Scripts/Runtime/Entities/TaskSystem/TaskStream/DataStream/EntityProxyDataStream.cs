using Anvil.CSharp.Logging;
using Anvil.CSharp.Reflection;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: Check all of these to see what is exposed on the outside
    public class EntityProxyDataStream<TInstance> : AbstractEntityProxyDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //TODO: Hide this stuff when collections checks are disabled
        private static readonly ProfilerMarker TYPED_MARKER = new ProfilerMarker(ProfilerCategory.Scripts, typeof(ConsolidateJob).GetReadableName(), MarkerFlags.Script);
        private static readonly FixedString64Bytes TYPED_NAME = new FixedString64Bytes(typeof(TInstance).GetReadableName());
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceWrapper<TInstance>>();

        private readonly CancelRequestsDataStream m_CancelRequestsDataStream;
        private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
        private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_IterationTarget;

        private NativeArray<JobHandle> m_ConsolidationDependencies;
        
        internal PendingCancelEntityProxyDataStream<TInstance> PendingCancelDataStream { get; }

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }

        internal override AbstractEntityProxyDataStream GetPendingCancelDataStream()
        {
            return PendingCancelDataStream;
        }

        internal EntityProxyDataStream(CancelRequestsDataStream cancelRequestsDataStream) : base()
        {
            m_CancelRequestsDataStream = cancelRequestsDataStream;
            m_Pending = new UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent);
            m_IterationTarget = new DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent,
                                                                                               Allocator.TempJob);
            PendingCancelDataStream = new PendingCancelEntityProxyDataStream<TInstance>();
            m_ConsolidationDependencies = new NativeArray<JobHandle>(4, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();
            
            m_ConsolidationDependencies.Dispose();
            PendingCancelDataStream.Dispose();

            //We don't own the m_CancelRequestsDataStream so we don't dispose it.
            
            
            base.DisposeSelf();
        }

        internal sealed override unsafe void* GetWriterPointer()
        {
            return m_Pending.AsWriter().GetBufferPointer();
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
            return new DataStreamReader<TInstance>(m_IterationTarget.AsDeferredJobArray());
        }

        internal DataStreamUpdater<TInstance> CreateDataStreamUpdater(DataStreamTargetResolver dataStreamTargetResolver)
        {
            return new DataStreamUpdater<TInstance>(m_Pending.AsWriter(),
                                                    m_IterationTarget.AsDeferredJobArray(),
                                                    dataStreamTargetResolver);
        }

        internal DataStreamWriter<TInstance> CreateDataStreamWriter(byte context)
        {
            return new DataStreamWriter<TInstance>(m_Pending.AsWriter(), context);
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        protected sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            m_ConsolidationDependencies[0] = dependsOn;
            m_ConsolidationDependencies[1] = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            m_ConsolidationDependencies[2] = PendingCancelDataStream.AccessController.AcquireAsync(AccessType.SharedWrite);
            m_ConsolidationDependencies[3] = m_CancelRequestsDataStream.AccessController.AcquireAsync(AccessType.SharedRead);
            
            ConsolidateJob consolidateJob = new ConsolidateJob(m_Pending,
                                                               m_IterationTarget,
                                                               PendingCancelDataStream.PendingWriter,
                                                               m_CancelRequestsDataStream.LookupRef,
                                                               TYPED_NAME,
                                                               TYPED_MARKER);
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(m_ConsolidationDependencies));

            AccessController.ReleaseAsync(consolidateHandle);
            PendingCancelDataStream.AccessController.ReleaseAsync(consolidateHandle);
            m_CancelRequestsDataStream.AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateJob : IJob
        {
            private const int UNSET_THREAD_INDEX = -1;
            
            [NativeSetThreadIndex] private readonly int m_NativeThreadIndex;
            [ReadOnly] private UnsafeParallelHashMap<EntityProxyInstanceID, byte> m_CancelRequestsForID;
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Iteration;
            [WriteOnly] private readonly UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer m_PendingCancelledWriter;
            private readonly FixedString64Bytes m_TypeName;
            private ProfilerMarker m_Marker;

            public ConsolidateJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                  DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> iteration,
                                  UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer pendingCancelledWriter,
                                  UnsafeParallelHashMap<EntityProxyInstanceID, byte> cancelRequests,
                                  FixedString64Bytes typeName,
                                  ProfilerMarker marker) : this()
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_PendingCancelledWriter = pendingCancelledWriter;
                m_CancelRequestsForID = cancelRequests;
                m_TypeName = typeName;
                m_Marker = marker;
                
                m_NativeThreadIndex = UNSET_THREAD_INDEX;
            }

            public void Execute()
            {
                m_Marker.Begin();
                m_Iteration.Clear();

                UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.LaneWriter pendingCancelledLaneWriter = m_PendingCancelledWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex));

                int pendingCount = m_Pending.Count();
                NativeArray<EntityProxyInstanceWrapper<TInstance>> iterationArray = m_Iteration.DeferredCreate(pendingCount);

                int liveIndex = 0;
                foreach (EntityProxyInstanceWrapper<TInstance> instance in m_Pending)
                {
                    if (m_CancelRequestsForID.ContainsKey(instance.InstanceID))
                    {
                        // Debug.Log($"Cancelling Instance with ID {instance.InstanceID} for {m_TypeName}");
                        pendingCancelledLaneWriter.Write(instance);
                    }
                    else
                    {
                        iterationArray[liveIndex] = instance;
                        liveIndex++;
                    }
                }
                
                //TODO: What happens if the cancellation needs to last more than one frame!? Not like we can ContinueOn because there is no Pending to write to. 
                //TODO: Might be better to have a separate PendingCancelDataStream inside this that we write to? 
                
                m_Iteration.ResetLengthTo(liveIndex);

                m_Pending.Clear();
                
                m_Marker.End();
            }
        }
    }
}
