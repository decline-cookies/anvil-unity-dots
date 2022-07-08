using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

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
    public class CancelVirtualData : AbstractVirtualData
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TKey"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        private static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<uint>();

        private UnsafeTypedStream<uint> m_Pending;
        private DeferredNativeArray<uint> m_IterationTarget;
        private UnsafeParallelHashMap<uint, bool> m_Lookup;

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }

        public int MaxElementsPerChunk
        {
            get => MAX_ELEMENTS_PER_CHUNK;
        }

        internal CancelVirtualData() : base()
        {
            m_Pending = new UnsafeTypedStream<uint>(Allocator.Persistent,
                                                    Allocator.TempJob);
            m_IterationTarget = new DeferredNativeArray<uint>(Allocator.Persistent,
                                                              Allocator.TempJob);
            m_Lookup = new UnsafeParallelHashMap<uint, bool>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
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

        internal VDLookupReader<bool> CreateVDLookupReader()
        {
            return new VDLookupReader<bool>(m_Lookup);
        }

        internal VDCancelWriter CreateVDCancelWriter()
        {
            return new VDCancelWriter(m_Pending.AsWriter());
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        internal sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn, CancelVirtualData cancelData)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateCancelLookupJob consolidateCancelLookupJob = new ConsolidateCancelLookupJob(m_Pending,
                                                                                                   m_IterationTarget,
                                                                                                   m_Lookup);
            JobHandle consolidateHandle = consolidateCancelLookupJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelLookupJob : IJob
        {
            private UnsafeTypedStream<uint> m_Pending;
            private DeferredNativeArray<uint> m_Iteration;
            private UnsafeParallelHashMap<uint, bool> m_Lookup;

            public ConsolidateCancelLookupJob(UnsafeTypedStream<uint> pending,
                                              DeferredNativeArray<uint> iteration,
                                              UnsafeParallelHashMap<uint, bool> lookup)
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_Lookup = lookup;
            }

            public void Execute()
            {
                //Clear previously consolidated 
                m_Lookup.Clear();
                m_Iteration.Clear();

                //Get the new counts
                int pendingCount = m_Pending.Count();
                
                Debug.Log($"EXECUTE CANCEL - {m_Pending.GetType()} - ConsolidateForFrame - Pending Count: {pendingCount}");

                //Allocate memory for arrays based on counts
                NativeArray<uint> iterationArray = m_Iteration.DeferredCreate(pendingCount);

                //Fast blit
                m_Pending.CopyTo(ref iterationArray);

                //Populate the lookup
                for (int i = 0; i < pendingCount; ++i)
                {
                    uint key = iterationArray[i];
                    m_Lookup.TryAdd(key, true);
                }

                //Clear pending for next frame
                m_Pending.Clear();
            }
        }
    }
}
