using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
    /// <typeparam name="TInstance">The type of data to store</typeparam>
    public class VirtualData<TKey, TInstance> : AbstractVirtualData
        where TKey : unmanaged, IEquatable<TKey>
        where TInstance : unmanaged, IKeyedData<TKey>
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<TInstance>();

        internal static VirtualData<TKey, TInstance> Create(params AbstractVirtualData[] sources)
        {
            VirtualData<TKey, TInstance> virtualData = new VirtualData<TKey, TInstance>();

            foreach (AbstractVirtualData source in sources)
            {
                virtualData.AddSource(source);
                source.AddResultDestination(virtualData);
            }

            return virtualData;
        }

        private UnsafeTypedStream<TInstance> m_Pending;
        private DeferredNativeArray<TInstance> m_IterationTarget;
        private UnsafeParallelHashMap<TKey, TInstance> m_Lookup;


        public DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }


        private VirtualData() : base()
        {
            m_Pending = new UnsafeTypedStream<TInstance>(Allocator.Persistent,
                                                         Allocator.TempJob);
            m_IterationTarget = new DeferredNativeArray<TInstance>(Allocator.Persistent,
                                                                   Allocator.TempJob);

            m_Lookup = new UnsafeParallelHashMap<TKey, TInstance>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();
            m_Lookup.Dispose();

            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

        internal VDReader<TInstance> CreateVDReader()
        {
            return new VDReader<TInstance>(m_IterationTarget.AsDeferredJobArray());
        }

        internal VDResultsDestination<TInstance> CreateVDResultsDestination()
        {
            return new VDResultsDestination<TInstance>(m_Pending.AsWriter());
        }

        internal VDUpdater<TKey, TInstance> CreateVDUpdater()
        {
            return new VDUpdater<TKey, TInstance>(m_Pending.AsWriter(),
                                                  m_IterationTarget.AsDeferredJobArray());
        }

        internal VDWriter<TInstance> CreateVDWriter()
        {
            return new VDWriter<TInstance>(m_Pending.AsWriter());
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        internal sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateLookupJob consolidateLookupJob = new ConsolidateLookupJob(m_Pending,
                                                                                 m_IterationTarget,
                                                                                 m_Lookup);
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
            private UnsafeTypedStream<TInstance> m_Pending;
            private DeferredNativeArray<TInstance> m_Iteration;
            private UnsafeParallelHashMap<TKey, TInstance> m_Lookup;

            public ConsolidateLookupJob(UnsafeTypedStream<TInstance> pending,
                                        DeferredNativeArray<TInstance> iteration,
                                        UnsafeParallelHashMap<TKey, TInstance> lookup)
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_Lookup = lookup;
            }

            public void Execute()
            {
                m_Lookup.Clear();
                m_Iteration.Clear();

                NativeArray<TInstance> iterationArray = m_Iteration.DeferredCreate(m_Pending.Count());
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();

                for (int i = 0; i < iterationArray.Length; ++i)
                {
                    TInstance value = iterationArray[i];
                    m_Lookup.TryAdd(value.Key, value);
                }
            }
        }
    }
}
