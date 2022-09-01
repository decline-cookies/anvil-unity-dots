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
    /// <typeparam name="TInstance">The type of data to store</typeparam>
    public class VirtualData<TInstance> : AbstractVirtualData
        where TInstance : unmanaged, IEntityProxyData
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<VDInstanceWrapper<TInstance>>();

        private const byte UNSET_RESULT_DESTINATION_TYPE = byte.MaxValue;

        private VDResultsDestinationLookup m_ResultsDestinationLookup;

        internal static VirtualData<TInstance> Create()
        {
            VirtualData<TInstance> virtualData = new VirtualData<TInstance>(UNSET_RESULT_DESTINATION_TYPE);
            return virtualData;
        }

        internal static VirtualData<TInstance> CreateAsResultsDestination<TResultDestinationType>(TResultDestinationType resultDestinationType, AbstractVirtualData source)
            where TResultDestinationType : Enum
        {
            byte value = (byte)(object)resultDestinationType;
            VirtualData<TInstance> resultDestinationData = new VirtualData<TInstance>(value);
            
            resultDestinationData.SetSource(source);
            source.AddResultDestination(value, resultDestinationData);
            
            return resultDestinationData;
        }

        private UnsafeTypedStream<VDInstanceWrapper<TInstance>> m_Pending;
        private DeferredNativeArray<VDInstanceWrapper<TInstance>> m_IterationTarget;
        private UnsafeParallelHashMap<VDContextID, VDInstanceWrapper<TInstance>> m_Lookup;


        public DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }


        private VirtualData(byte resultDestinationType) : base(resultDestinationType)
        {
            m_Pending = new UnsafeTypedStream<VDInstanceWrapper<TInstance>>(Allocator.Persistent);
            m_IterationTarget = new DeferredNativeArray<VDInstanceWrapper<TInstance>>(Allocator.Persistent,
                                                                                    Allocator.TempJob);

            m_Lookup = new UnsafeParallelHashMap<VDContextID, VDInstanceWrapper<TInstance>>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            if (m_ResultsDestinationLookup.IsCreated)
            {
                m_ResultsDestinationLookup.Dispose();
            }
            
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();
            m_Lookup.Dispose();

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

        internal VDReader<TInstance> CreateVDReader()
        {
            return new VDReader<TInstance>(m_IterationTarget.AsDeferredJobArray());
        }

        internal VDResultsDestination<TInstance> CreateVDResultsDestination()
        {
            return new VDResultsDestination<TInstance>(m_Pending.AsWriter());
        }

        internal VDUpdater<TInstance> CreateVDUpdater(uint context)
        {
            Debug_EnsureContextIsSet(context);
            return new VDUpdater<TInstance>(m_Pending.AsWriter(),
                                                  m_IterationTarget.AsDeferredJobArray());
        }

        internal VDWriter<TInstance> CreateVDWriter(uint context)
        {
            Debug_EnsureContextIsSet(context);
            return new VDWriter<TInstance>(m_Pending.AsWriter(), context);
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
            private UnsafeTypedStream<VDInstanceWrapper<TInstance>> m_Pending;
            private DeferredNativeArray<VDInstanceWrapper<TInstance>> m_Iteration;
            private UnsafeParallelHashMap<VDContextID, VDInstanceWrapper<TInstance>> m_Lookup;

            public ConsolidateLookupJob(UnsafeTypedStream<VDInstanceWrapper<TInstance>> pending,
                                        DeferredNativeArray<VDInstanceWrapper<TInstance>> iteration,
                                        UnsafeParallelHashMap<VDContextID, VDInstanceWrapper<TInstance>> lookup)
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_Lookup = lookup;
            }

            public void Execute()
            {
                m_Lookup.Clear();
                m_Iteration.Clear();

                NativeArray<VDInstanceWrapper<TInstance>> iterationArray = m_Iteration.DeferredCreate(m_Pending.Count());
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();

                for (int i = 0; i < iterationArray.Length; ++i)
                {
                    VDInstanceWrapper<TInstance> value = iterationArray[i];
                    m_Lookup.TryAdd(value.ID, value);
                }
            }
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureContextIsSet(uint context)
        {
            //TODO: Deal with actually handling this
            if (context == IDProvider.UNSET_ID)
            {
                throw new InvalidOperationException($"Context for {typeof(TInstance)} is not set!");
            }
        }
    }
}
