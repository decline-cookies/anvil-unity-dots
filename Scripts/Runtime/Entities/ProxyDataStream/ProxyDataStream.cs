using Anvil.CSharp.Core;
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
    /// <typeparam name="TData">The type of data to store</typeparam>
    public class ProxyDataStream<TData> : AbstractAnvilBase
        where TData : unmanaged, IProxyData
    {
        //TODO: RE-ENABLE IF NEEDED
        // internal static readonly BulkScheduleDelegate<AbstractProxyDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractProxyDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);


        /// <summary>
        /// The number of elements of <typeparamref name="TData"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<ProxyDataWrapper<TData>>();

        private UnsafeTypedStream<ProxyDataWrapper<TData>> m_Pending;
        private DeferredNativeArray<ProxyDataWrapper<TData>> m_IterationTarget;

        //TODO: Lock down to internal again
        public AccessController AccessController { get; }

        public DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }

        //TODO: Rename to something better. VirtualData is ambiguous between one instance of data or the collection. This is more of a stream. Think on it.
        //TODO: Split VirtualData into two pieces of functionality.
        //TODO: 1. Data collection independent of the TaskDrivers all about Wide/Narrow and load balancing. 
        //TODO: 2. A mechanism to handle the branching from Data to a Result type
        //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/52/files#r960787785
        public ProxyDataStream() : base()
        {
            //TODO: Could split the data into definitions via Attributes or some other mechanism to set up the relationships. Then a "baking" into the actual structures. 
            //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/52/files#r960764532
            //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/52/files#r960737069
            AccessController = new AccessController();

            m_Pending = new UnsafeTypedStream<ProxyDataWrapper<TData>>(Allocator.Persistent);
            m_IterationTarget = new DeferredNativeArray<ProxyDataWrapper<TData>>(Allocator.Persistent,
                                                                                 Allocator.TempJob);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();

            AccessController.Dispose();
            base.DisposeSelf();
        }

        internal unsafe void* GetWriterPointer()
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

        internal PDSUpdater<TData> CreateVDUpdater(byte context)
        {
            return new PDSUpdater<TData>(m_Pending.AsWriter(),
                                         m_IterationTarget.AsDeferredJobArray());
        }

        //TODO: Lock down to internal again
        public PDSWriter<TData> CreatePDSWriter(byte context)
        {
            //TODO: RE-ENABLE IF NEEDED
            // Debug_EnsureContextIsSet(context);
            return new PDSWriter<TData>(m_Pending.AsWriter(), context);
        }

        //TODO: RE-ENABLE IF NEEDED
        // internal VDResultsDestinationLookup GetOrCreateVDResultsDestinationLookup()
        // {
        //     if (!m_ResultsDestinationLookup.IsCreated)
        //     {
        //         m_ResultsDestinationLookup = new VDResultsDestinationLookup(ResultDestinations);
        //     }
        //
        //     return m_ResultsDestinationLookup;
        // }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        internal JobHandle ConsolidateForFrame(JobHandle dependsOn)
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
            private UnsafeTypedStream<ProxyDataWrapper<TData>> m_Pending;
            private DeferredNativeArray<ProxyDataWrapper<TData>> m_Iteration;

            public ConsolidateLookupJob(UnsafeTypedStream<ProxyDataWrapper<TData>> pending,
                                        DeferredNativeArray<ProxyDataWrapper<TData>> iteration)
            {
                m_Pending = pending;
                m_Iteration = iteration;
            }

            public void Execute()
            {
                m_Iteration.Clear();

                NativeArray<ProxyDataWrapper<TData>> iterationArray = m_Iteration.DeferredCreate(m_Pending.Count());
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();
            }
        }
    }
}
