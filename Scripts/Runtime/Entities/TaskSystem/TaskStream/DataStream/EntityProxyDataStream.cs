using Anvil.CSharp.Reflection;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityProxyDataStream<TInstance> : AbstractEntityProxyDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceWrapper<TInstance>>();

        private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
        private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_IterationTarget;

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }

        internal EntityProxyDataStream() : base()
        {
            m_Pending = new UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent);
            m_IterationTarget = new DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent,
                                                                                         Allocator.TempJob);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();
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

        internal DataStreamUpdater<TInstance> CreateDataStreamUpdater(CancelRequestsReader cancelRequestsReader,
                                                                      EntityProxyDataStream<TInstance> pendingCancelDataStream,
                                                                      DataStreamTargetResolver dataStreamTargetResolver)
        {
            return new DataStreamUpdater<TInstance>(m_Pending.AsWriter(),
                                                    m_IterationTarget.AsDeferredJobArray(),
                                                    cancelRequestsReader,
                                                    pendingCancelDataStream.m_Pending.AsWriter(),
                                                    dataStreamTargetResolver);
        }

        internal DataStreamCancellationUpdater<TInstance> CreateDataStreamCancellationUpdater(DataStreamTargetResolver dataStreamTargetResolver)
        {
            return new DataStreamCancellationUpdater<TInstance>(m_Pending.AsWriter(),
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
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateJob consolidateJob = new ConsolidateJob(m_Pending,
                                                               m_IterationTarget,
                                                               new FixedString64Bytes(typeof(TInstance).GetReadableName()));
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateJob : IJob
        {
            private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
            private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Iteration;
            private readonly FixedString64Bytes m_TypeName;

            public ConsolidateJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                  DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> iteration,
                                  FixedString64Bytes typeName)
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_TypeName = typeName;
            }

            public void Execute()
            {
                m_Iteration.Clear();

                NativeArray<EntityProxyInstanceWrapper<TInstance>> iterationArray = m_Iteration.DeferredCreate(m_Pending.Count());
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();

                if (iterationArray.Length > 0)
                {
                    UnityEngine.Debug.Log($"Consolidate for {m_TypeName} complete with {iterationArray.Length}");
                }
            }
        }
    }
}
