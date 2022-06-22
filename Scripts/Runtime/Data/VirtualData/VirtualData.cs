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
    /// This class represents wrapped collections of data and manages them for use in Jobs.
    /// </summary>
    /// <remarks>
    /// In Unity's ECS, data is stored on <see cref="Entity"/>'s via <see cref="IComponentBase"/> structs.
    /// Unity's <see cref="SystemBase"/>'s handle the dependencies on the different sorts of data that are needed
    /// for a given update call depending on the <see cref="EntityQuery"/>s used and Jobs scheduled.
    ///
    /// There are often cases where the overhead of using Entities doesn't make sense. The data is not persistent
    /// and should exist for a period of time and then cease to exist. While this can be accomplished by Adding
    /// or Removing Components from an Entity or spawning/destroying new entities with those components, it results
    /// in a structural change which gets resolved at a sync point and happens on the main thread.
    ///
    /// Instead, using <see cref="VirtualData{TKey,TInstance}"/> can alleviate these issues while working in a similar
    /// manner. Additional benefits are:
    /// 
    /// - Allowing for Shared Write
    /// - - Multiple different jobs can write to the Pending collection at the same time.
    /// - Fast reading via iteration or lookup
    /// - The ability for instances of the data to write a result to different result destinations.
    /// - - This gives implicit grouping of the data while still allowing for processing the overall set of data
    ///     as one large set.
    /// - - Getting access to writing to the results is handled automatically.
    /// </remarks>
    /// <typeparam name="TKey">The type of key to use to lookup data. Usually <see cref="Entity"/></typeparam>
    /// <typeparam name="TInstance">The type of data to store</typeparam>
    public class VirtualData<TKey, TInstance> : AbstractAnvilBase,
                                                IVirtualData
        where TKey : struct, IEquatable<TKey>
        where TInstance : struct, ILookupData<TKey>
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

        private readonly HashSet<IVirtualData> m_Sources = new HashSet<IVirtualData>();
        private readonly HashSet<IVirtualData> m_ResultDestinations = new HashSet<IVirtualData>();
        private readonly AccessController m_AccessController;

        private UnsafeTypedStream<TInstance> m_Pending;
        private DeferredNativeArray<TInstance> m_Iteration;
        private UnsafeHashMap<TKey, TInstance> m_Lookup;

        
        public DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_Iteration.ScheduleInfo;
        }

        AccessController IVirtualData.AccessController
        {
            get => m_AccessController;
        }

        private VirtualData()
        {
            m_AccessController = new AccessController();
            m_Pending = new UnsafeTypedStream<TInstance>(Allocator.Persistent,
                                                         Allocator.TempJob);
            m_Iteration = new DeferredNativeArray<TInstance>(Allocator.Persistent,
                                                             Allocator.TempJob);

            m_Lookup = new UnsafeHashMap<TKey, TInstance>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            m_Pending.Dispose();
            m_Iteration.Dispose();
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

        //TODO: Add Serialization and Deserialization functions to hook into our serialization framework

        //*************************************************************************************************************
        // RELATIONSHIPS
        //*************************************************************************************************************

        void IVirtualData.AddResultDestination(IVirtualData resultData)
        {
            AddResultDestination(resultData);
        }

        private void AddResultDestination(IVirtualData resultData)
        {
            m_ResultDestinations.Add(resultData);
        }

        void IVirtualData.RemoveResultDestination(IVirtualData resultData)
        {
            RemoveResultDestination(resultData);
        }

        private void RemoveResultDestination(IVirtualData resultData)
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

        JobHandle IVirtualData.AcquireForUpdate()
        {
            return AcquireForUpdate();
        }

        private JobHandle AcquireForUpdate()
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
            int index = 1;
            foreach (IVirtualData destinationData in m_ResultDestinations)
            {
                allDependencies[index] = destinationData.AccessController.AcquireAsync(AccessType.SharedWrite);
                index++;
            }

            return JobHandle.CombineDependencies(allDependencies);
        }

        void IVirtualData.ReleaseForUpdate(JobHandle releaseAccessDependency)
        {
            ReleaseForUpdate(releaseAccessDependency);
        }

        private void ReleaseForUpdate(JobHandle releaseAccessDependency)
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

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

        internal VDJobReader<TInstance> CreateVDJobReader()
        {
            return new VDJobReader<TInstance>(m_Iteration.AsDeferredJobArray());
        }

        internal VDJobResultsDestination<TInstance> CreateVDJobResultsDestination()
        {
            return new VDJobResultsDestination<TInstance>(m_Pending.AsWriter());
        }

        internal VDJobUpdater<TKey, TInstance> CreateVDJobUpdater()
        {
            return new VDJobUpdater<TKey, TInstance>(m_Pending.AsWriter(),
                                                     m_Iteration.AsDeferredJobArray());
        }

        internal VDJobWriter<TInstance> CreateVDJobWriter()
        {
            return new VDJobWriter<TInstance>(m_Pending.AsWriter());
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************

        JobHandle IVirtualData.ConsolidateForFrame(JobHandle dependsOn)
        {
            return ConsolidateForFrame(dependsOn);
        }

        private JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateLookupJob consolidateLookupJob = new ConsolidateLookupJob(m_Pending,
                                                                                 m_Iteration,
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
