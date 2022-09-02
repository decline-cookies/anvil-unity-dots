using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
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
    /// <typeparam name="TData">The type of data to store</typeparam>
    public class ProxyDataStream<TData> : AbstractProxyDataStream
        where TData : unmanaged, IEntityProxyData
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TData"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<PDWrapper<TData>>();

        private UnsafeTypedStream<PDWrapper<TData>> m_Pending;
        private DeferredNativeArray<PDWrapper<TData>> m_IterationTarget;

        public DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }

        public ProxyDataStream() : base()
        {
            m_Pending = new UnsafeTypedStream<PDWrapper<TData>>(Allocator.Persistent);
            m_IterationTarget = new DeferredNativeArray<PDWrapper<TData>>(Allocator.Persistent,
                                                                                 Allocator.TempJob);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();

            base.DisposeSelf();
        }

        internal override unsafe void* GetWriterPointer()
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

        internal PDSReader<TData> CreatePDSReader()
        {
            return new PDSReader<TData>(m_IterationTarget.AsDeferredJobArray());
        }

        internal VDResultsDestination<TData> CreateVDResultsDestination()
        {
            return new VDResultsDestination<TData>(m_Pending.AsWriter());
        }

        internal VDUpdater<TData> CreateVDUpdater(byte context)
        {
            return new VDUpdater<TData>(m_Pending.AsWriter(),
                                        m_IterationTarget.AsDeferredJobArray());
        }

        internal PDSWriter<TData> CreateVDWriter(byte context)
        {
            Debug_EnsureContextIsSet(context);
            return new PDSWriter<TData>(m_Pending.AsWriter(), context);
        }

        internal VDResultsDestinationLookup GetOrCreateVDResultsDestinationLookup()
        {
            if (!m_ResultsDestinationLookup.IsCreated)
            {
                m_ResultsDestinationLookup = new VDResultsDestinationLookup(ResultDestinations);
            }

            return m_ResultsDestinationLookup;
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        internal sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateLookupJob consolidateLookupJob = new ConsolidateLookupJob(m_Pending,
                                                                                 m_IterationTarget);
            JobHandle consolidateHandle = consolidateLookupJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateLookupJob : IJob
        {
            private UnsafeTypedStream<PDWrapper<TData>> m_Pending;
            private DeferredNativeArray<PDWrapper<TData>> m_Iteration;

            public ConsolidateLookupJob(UnsafeTypedStream<PDWrapper<TData>> pending,
                                        DeferredNativeArray<PDWrapper<TData>> iteration)
            {
                m_Pending = pending;
                m_Iteration = iteration;
            }

            public void Execute()
            {
                m_Iteration.Clear();

                NativeArray<PDWrapper<TData>> iterationArray = m_Iteration.DeferredCreate(m_Pending.Count());
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();
            }
        }
    }
}
