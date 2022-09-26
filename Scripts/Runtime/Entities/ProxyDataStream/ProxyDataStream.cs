using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    //TODO: Revisit Docs
    /// <summary>
    /// Represents wrapped collections of data and manages them for use in Jobs.
    /// </summary>
    /// <remarks>
    /// In Unity's ECS, data is stored on <see cref="Entity"/>'s via <see cref="IComponentBase"/> structs.
    /// Unity's <see cref="SystemBase"/>'s handle the dependencies on the different sorts of data that are needed
    /// for a given update call depending on the <see cref="EntityQuery"/>s used and Jobs scheduled.
    ///
    /// When to use VirtualData over Entities+Components?
    /// The general rule of thumb is that using VirtualData will be easier to work with and faster to execute.
    /// Spawning and destroying Entities+Components can lead to chunk fragmentation is the lifecycles are variable with
    /// some lasting a short time while others last longer. It also results in a structural change which gets resolved
    /// at a sync point on the main thread.
    ///
    /// Additional VirtualData benefits are:
    /// - Allowing for parallel writing
    ///   - Multiple different jobs can write to the Pending collection at the same time.
    /// - Fast reading via iteration or individual lookup
    /// - The ability for each instance of the data to write its result to a different result destination.
    ///   - This gives implicit grouping of the data while still allowing for processing the overall set of data
    ///     as one large set. (Ex: Timers update as one set but complete back to different destinations)
    ///   - Getting write to result destinations is handled automatically.
    /// </remarks>
    /// <typeparam name="TKey">
    /// The type of key to use to lookup data. Usually <see cref="Entity"/> if this is being used as an
    /// alternative to adding component data to an <see cref="Entity"/>
    /// </typeparam>
    /// <typeparam name="TInstance">The type of data to store</typeparam>
    public class ProxyDataStream<TInstance> : AbstractProxyDataStream
        where TInstance : unmanaged, IProxyInstance
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<ProxyInstanceWrapper<TInstance>>();

        private UnsafeTypedStream<ProxyInstanceWrapper<TInstance>> m_Pending;
        private DeferredNativeArray<ProxyInstanceWrapper<TInstance>> m_IterationTarget;

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }

        internal ProxyDataStream() : base()
        {
            m_Pending = new UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>(Allocator.Persistent);
            m_IterationTarget = new DeferredNativeArray<ProxyInstanceWrapper<TInstance>>(Allocator.Persistent,
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

        //TODO: Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************
        internal DataStreamReader<TInstance> CreateDataStreamReader()
        {
            return new DataStreamReader<TInstance>(m_IterationTarget.AsDeferredJobArray());
        }

        internal DataStreamUpdater<TInstance> CreateDataStreamUpdater(CancelRequestsReader cancelRequestsReader,
                                                                      ProxyDataStream<TInstance> pendingCancelDataStream,
                                                                      DataStreamChannelResolver dataStreamChannelResolver)
        {
            return new DataStreamUpdater<TInstance>(m_Pending.AsWriter(),
                                                    m_IterationTarget.AsDeferredJobArray(),
                                                    cancelRequestsReader,
                                                    pendingCancelDataStream.m_Pending.AsWriter(),
                                                    dataStreamChannelResolver);
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
                                                               m_IterationTarget);
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
            private UnsafeTypedStream<ProxyInstanceWrapper<TInstance>> m_Pending;
            private DeferredNativeArray<ProxyInstanceWrapper<TInstance>> m_Iteration;

            public ConsolidateJob(UnsafeTypedStream<ProxyInstanceWrapper<TInstance>> pending,
                                  DeferredNativeArray<ProxyInstanceWrapper<TInstance>> iteration)
            {
                m_Pending = pending;
                m_Iteration = iteration;
            }

            public void Execute()
            {
                m_Iteration.Clear();

                NativeArray<ProxyInstanceWrapper<TInstance>> iterationArray = m_Iteration.DeferredCreate(m_Pending.Count());
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();
            }
        }
    }
}
