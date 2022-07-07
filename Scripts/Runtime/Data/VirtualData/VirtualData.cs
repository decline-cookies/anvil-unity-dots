using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
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
    /// <typeparam name="TInstance">The type of data to store</typeparam>
    public class VirtualData<TKey, TInstance> : AbstractVirtualData<TKey>
        where TKey : unmanaged, IEquatable<TKey>
        where TInstance : unmanaged, IKeyedData<TKey>
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<TInstance>();

        internal static VirtualData<TKey, TInstance> Create(params AbstractVirtualData<TKey>[] sources)
        {
            VirtualData<TKey, TInstance> virtualData = new VirtualData<TKey, TInstance>();

            foreach (AbstractVirtualData<TKey> source in sources)
            {
                virtualData.AddSource(source);
                source.AddResultDestination(virtualData);
            }

            return virtualData;
        }

        private UnsafeTypedStream<TInstance> m_Pending;
        private DeferredNativeArray<TInstance> m_IterationTarget;
        private DeferredNativeArray<TInstance> m_CancelledIterationTarget;
        private UnsafeParallelHashMap<TKey, TInstance> m_Lookup;

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }
        
        private VirtualData() : base()
        {
            m_Pending = new UnsafeTypedStream<TInstance>(Allocator.Persistent,
                                                         Allocator.TempJob);
            m_IterationTarget = new DeferredNativeArray<TInstance>(Allocator.Persistent,
                                                                   Allocator.TempJob);
            m_CancelledIterationTarget = new DeferredNativeArray<TInstance>(Allocator.Persistent,
                                                                            Allocator.TempJob);
            m_Lookup = new UnsafeParallelHashMap<TKey, TInstance>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();
            m_CancelledIterationTarget.Dispose();
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
        
        internal VDLookupReader<TKey, TInstance> CreateVDLookupReader()
        {
            return new VDLookupReader<TKey, TInstance>(m_Lookup);
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
        internal sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn, CancelVirtualData<TKey> cancelData)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateLookupJob consolidateLookupJob = new ConsolidateLookupJob(m_Pending,
                                                                                 m_IterationTarget,
                                                                                 m_Lookup,
                                                                                 cancelData.CreateVDLookupReader(),
                                                                                 m_CancelledIterationTarget);
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
            private DeferredNativeArray<TInstance> m_IterationTarget;
            private UnsafeParallelHashMap<TKey, TInstance> m_Lookup;
            private VDLookupReader<TKey, bool> m_CancelledLookup;
            private DeferredNativeArray<TInstance> m_CancelledIterationTarget;

            public ConsolidateLookupJob(UnsafeTypedStream<TInstance> pending,
                                        DeferredNativeArray<TInstance> iterationTarget,
                                        UnsafeParallelHashMap<TKey, TInstance> lookup,
                                        VDLookupReader<TKey, bool> cancelledLookup,
                                        DeferredNativeArray<TInstance> cancelledIterationTarget)
            {
                m_Pending = pending;
                m_IterationTarget = iterationTarget;
                m_Lookup = lookup;
                m_CancelledLookup = cancelledLookup;
                m_CancelledIterationTarget = cancelledIterationTarget;
            }

            public void Execute()
            {
                //Clear previously consolidated 
                m_Lookup.Clear();
                m_IterationTarget.Clear();
                m_CancelledIterationTarget.Clear();

                //Get the new counts
                int pendingCount = m_Pending.Count();
                int pendingCancelledCount = m_CancelledLookup.Count();
                
                //Take optimized path if possible
                if (pendingCancelledCount == 0)
                {
                    ConsolidateWithoutCancel(pendingCount);
                }
                else
                {
                    ConsolidateWithCancel(pendingCount, pendingCancelledCount);
                }
                
                //Clear pending for next frame
                m_Pending.Clear();
            }

            private void ConsolidateWithoutCancel(int pendingCount)
            {
                //Allocate memory for array based on counts
                NativeArray<TInstance> iterationArray = m_IterationTarget.DeferredCreate(pendingCount);
                
                //Fast blit
                m_Pending.CopyTo(ref iterationArray);
                
                //Populate the lookup
                for (int i = 0; i < pendingCount; ++i)
                {
                    TInstance instance = iterationArray[i];
                    m_Lookup.TryAdd(instance.Key, instance);
                }
            }

            private void ConsolidateWithCancel(int pendingCount, int pendingCancelledCount)
            {
                //Allocate memory for array based on counts
                NativeArray<TInstance> iterationArray = m_IterationTarget.DeferredCreate(pendingCount - pendingCancelledCount);
                NativeArray<TInstance> cancelledIterationArray = m_CancelledIterationTarget.DeferredCreate(pendingCancelledCount);

                //Build up the surviving iteration array and lookup
                int iterationIndex = 0;
                int cancelledIterationIndex = 0;
                for (int laneIndex = 0; laneIndex < m_Pending.LaneCount; ++laneIndex)
                {
                    UnsafeTypedStream<TInstance>.LaneReader laneReader = m_Pending.AsLaneReader(laneIndex);
                    for (int i = 0; i < laneReader.Count; ++i)
                    {
                        TInstance instance = laneReader.Read();
                        if (m_CancelledLookup.ContainsKey(instance.Key))
                        {
                            cancelledIterationArray[cancelledIterationIndex] = instance;
                            cancelledIterationIndex++;
                            continue;
                        }

                        iterationArray[iterationIndex] = instance;
                        m_Lookup.TryAdd(instance.Key, instance);
                        iterationIndex++;
                    }
                }
            }
        }
    }
}
