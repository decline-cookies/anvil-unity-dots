using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
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
    public class VirtualData<TKey, TInstance> : AbstractAnvilBase,
                                                IVirtualData
        where TKey : struct, IEquatable<TKey>
        where TInstance : struct, IKeyedData<TKey>
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<TInstance>();

        internal static VirtualData<TKey, TInstance> Create(params IVirtualData[] sources)
        {
            VirtualData<TKey, TInstance> virtualData = new VirtualData<TKey, TInstance>();

            foreach (IVirtualData source in sources)
            {
                virtualData.AddSource(source);
                source.AddResultDestination(virtualData);
            }

            return virtualData;
        }

        private readonly List<IVirtualData> m_Sources;
        private readonly List<IVirtualData> m_ResultDestinations;
        private readonly AccessController m_AccessController;

        private UnsafeTypedStream<TInstance> m_Pending;
        private DeferredNativeArray<TInstance> m_IterationTarget;
        private UnsafeHashMap<TKey, TInstance> m_Lookup;

        AccessController IVirtualData.AccessController
        {
            get => m_AccessController;
        }
        
        public DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }


        private VirtualData()
        {
            m_Sources = new List<IVirtualData>();
            m_ResultDestinations = new List<IVirtualData>();
            m_AccessController = new AccessController();
            m_Pending = new UnsafeTypedStream<TInstance>(Allocator.Persistent,
                                                         Allocator.TempJob);
            m_IterationTarget = new DeferredNativeArray<TInstance>(Allocator.Persistent,
                                                                   Allocator.TempJob);

            m_Lookup = new UnsafeHashMap<TKey, TInstance>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            m_Pending.Dispose();
            m_IterationTarget.Dispose();
            m_Lookup.Dispose();

            RemoveFromSources();
            m_ResultDestinations.Clear();
            m_Sources.Clear();

            m_AccessController.Dispose();

            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // RELATIONSHIPS
        //*************************************************************************************************************

        void IVirtualData.AddResultDestination(IVirtualData resultData)
        {
            m_ResultDestinations.Add(resultData);
        }
        
        void IVirtualData.RemoveResultDestination(IVirtualData resultData)
        {
            m_ResultDestinations.Remove(resultData);
        }

        private void AddSource(IVirtualData sourceData)
        {
            m_Sources.Add(sourceData);
        }

        private void RemoveSource(IVirtualData sourceData)
        {
            m_Sources.Remove(sourceData);
        }

        private void RemoveFromSources()
        {
            foreach (IVirtualData sourceData in m_Sources)
            {
                sourceData.RemoveResultDestination(this);
            }
        }

        //*************************************************************************************************************
        // ACCESS
        //*************************************************************************************************************

        JobHandle IVirtualData.AcquireForUpdateAsync()
        {
            JobHandle exclusiveWrite = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            if (m_ResultDestinations.Count == 0)
            {
                return exclusiveWrite;
            }

            //Get write access to all possible channels that we can write a response to.
            //+1 to include the exclusive write
            NativeArray<JobHandle> allDependencies = new NativeArray<JobHandle>(m_ResultDestinations.Count + 1, Allocator.Temp);
            allDependencies[0] = exclusiveWrite;
            for (int i = 1; i < allDependencies.Length; ++i)
            {
                IVirtualData destinationData = m_ResultDestinations[i];
                allDependencies[i] = destinationData.AccessController.AcquireAsync(AccessType.SharedWrite);
            }

            return JobHandle.CombineDependencies(allDependencies);
        }

        void IVirtualData.ReleaseForUpdateAsync(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);

            if (m_ResultDestinations.Count == 0)
            {
                return;
            }

            foreach (IVirtualData destinationData in m_ResultDestinations)
            {
                destinationData.AccessController.ReleaseAsync(releaseAccessDependency);
            }
        }

        void IVirtualData.AcquireForUpdate()
        {
            m_AccessController.Acquire(AccessType.ExclusiveWrite);

            foreach (IVirtualData destinationData in m_ResultDestinations)
            {
                destinationData.AccessController.Acquire(AccessType.SharedWrite);
            }
        }

        void IVirtualData.ReleaseForUpdate()
        {
            m_AccessController.Release();
            
            foreach (IVirtualData destinationData in m_ResultDestinations)
            {
                destinationData.AccessController.Release();
            }
        }

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
        JobHandle IVirtualData.ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateLookupJob consolidateLookupJob = new ConsolidateLookupJob(m_Pending,
                                                                                 m_IterationTarget,
                                                                                 m_Lookup);
            JobHandle consolidateHandle = consolidateLookupJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            m_AccessController.ReleaseAsync(consolidateHandle);

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
            private UnsafeHashMap<TKey, TInstance> m_Lookup;

            public ConsolidateLookupJob(UnsafeTypedStream<TInstance> pending,
                                        DeferredNativeArray<TInstance> iteration,
                                        UnsafeHashMap<TKey, TInstance> lookup)
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
