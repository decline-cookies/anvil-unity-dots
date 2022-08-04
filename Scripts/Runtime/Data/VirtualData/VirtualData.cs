using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

//TODO: DISCUSS - Namespace

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
        where TInstance : unmanaged, IKeyedData
    {
        /// <summary>
        /// The number of elements of <typeparamref name="TInstance"/> that can fit into a chunk (16kb)
        /// This is useful for deciding on batch sizes.
        /// </summary>
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<TInstance>();

        private UnsafeTypedStream<TInstance> m_Pending;

        private DeferredNativeArray<TInstance> m_IterationTarget;
        private DeferredNativeArray<TInstance> m_CancelledIterationTarget;

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }

        internal DeferredNativeArrayScheduleInfo CancelledScheduleInfo
        {
            get => m_CancelledIterationTarget.ScheduleInfo;
        }

        internal VirtualData(VirtualDataIntent intent, params AbstractVirtualData[] sources) : base(intent)
        {
            m_Pending = new UnsafeTypedStream<TInstance>(Allocator.Persistent,
                                                         Allocator.Persistent);

            m_IterationTarget = new DeferredNativeArray<TInstance>(Allocator.Persistent,
                                                                   Allocator.TempJob);
            m_CancelledIterationTarget = new DeferredNativeArray<TInstance>(Allocator.Persistent,
                                                                            Allocator.TempJob);

            foreach (AbstractVirtualData source in sources)
            {
                AddSource(source);
                source.AddResultDestination(this);
            }
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();
            m_CancelledIterationTarget.Dispose();

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

        internal VDReader<TInstance> CreateVDCancelledReader()
        {
            return new VDReader<TInstance>(m_CancelledIterationTarget.AsDeferredJobArray());
        }

        internal VDResultsDestination<TInstance> CreateVDResultsDestination()
        {
            return new VDResultsDestination<TInstance>(m_Pending.AsWriter());
        }

        internal VDUpdater<TInstance> CreateVDUpdater()
        {
            return new VDUpdater<TInstance>(m_Pending.AsWriter(),
                                            m_IterationTarget.AsDeferredJobArray());
        }

        internal VDWriter<TInstance> CreateVDWriter(int context)
        {
            Debug_EnsureContextIsSet(context);
            return new VDWriter<TInstance>(m_Pending.AsWriter(), context);
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        internal sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn, CancelData cancelData)
        {
            dependsOn = JobHandle.CombineDependencies(AccessController.AcquireAsync(AccessType.ExclusiveWrite),
                                                      dependsOn);
            
            ConsolidateJob consolidateJob = new ConsolidateJob(m_Pending,
                                                               cancelData.CreateVDLookupReader(),
                                                               m_IterationTarget,
                                                               m_CancelledIterationTarget);
            dependsOn = consolidateJob.Schedule(dependsOn);

            AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************
        
        [BurstCompile]
        private struct ConsolidateJob : IAnvilJob
        {
            [ReadOnly] private VDLookupReader<bool> m_CancelLookup;
            private UnsafeTypedStream<TInstance> m_Pending;
            private DeferredNativeArray<TInstance> m_IterationTarget;
            private DeferredNativeArray<TInstance> m_CancelledIterationTarget;

            private int m_CancelLookupCount;

            public ConsolidateJob(UnsafeTypedStream<TInstance> pending,
                                  VDLookupReader<bool> cancelLookup,
                                  DeferredNativeArray<TInstance> iterationTarget,
                                  DeferredNativeArray<TInstance> cancelledIterationTarget)
            {
                m_Pending = pending;
                m_CancelLookup = cancelLookup;
                m_IterationTarget = iterationTarget;
                m_CancelledIterationTarget = cancelledIterationTarget;

                m_CancelLookupCount = 0;
            }

            public void InitForThread(int nativeThreadIndex)
            {
                m_CancelLookupCount = m_CancelLookup.Count();
            }

            public void Execute()
            {
                //Clear previously consolidated 
                m_IterationTarget.Clear();
                m_CancelledIterationTarget.Clear();

                NativeArray<TInstance> pendingArray = m_Pending.ToNativeArray(Allocator.Temp);

                //Clear pending for next frame
                m_Pending.Clear();
                
                int cancelIndex = 0;
                for (int i = 0; i < pendingArray.Length; ++i)
                {
                    TInstance instance = pendingArray[i];
                    if (m_CancelLookupCount <= 0
                     || !m_CancelLookup.ContainsKey(instance.ContextID))
                    {
                        continue;
                    }
                    
                    TInstance swapInstance = pendingArray[cancelIndex];
                    pendingArray[i] = swapInstance;
                    pendingArray[cancelIndex] = instance;
                    cancelIndex++;
                }

                int liveLength = pendingArray.Length - cancelIndex;

                NativeArray<TInstance> cancelArray = m_CancelledIterationTarget.DeferredCreate(cancelIndex);
                NativeArray<TInstance>.Copy(pendingArray, 
                                            0, 
                                            cancelArray, 
                                            0, 
                                            cancelIndex);

                NativeArray<TInstance> liveArray = m_IterationTarget.DeferredCreate(liveLength);
                NativeArray<TInstance>.Copy(pendingArray, 
                                            cancelIndex, 
                                            liveArray, 
                                            0, 
                                            liveLength);
            }
        }

        [BurstCompile]
        internal struct KeepPersistentJob : IAnvilJobForDefer
        {
            // BEGIN SCHEDULING -----------------------------------------------------------------------------
            internal static JobHandle Schedule(JobHandle dependsOn, TaskWorkData jobTaskWorkData, IScheduleInfo scheduleInfo)
            {
                KeepPersistentJob keepPersistentJob = new KeepPersistentJob(jobTaskWorkData.GetVDUpdaterAsync<TInstance>());
                return keepPersistentJob.ScheduleParallel(scheduleInfo.DeferredNativeArrayScheduleInfo,
                                                          scheduleInfo.BatchSize,
                                                          dependsOn);
            }
            // END SCHEDULING -------------------------------------------------------------------------------


            private VDUpdater<TInstance> m_Updater;

            private KeepPersistentJob(VDUpdater<TInstance> updater)
            {
                m_Updater = updater;
            }

            public void InitForThread(int nativeThreadIndex)
            {
                m_Updater.InitForThread(nativeThreadIndex);
            }

            public void Execute(int index)
            {
                TInstance instance = m_Updater[index];
                instance.ContinueOn(ref m_Updater);
            }
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureContextIsSet(int context)
        {
            if (context == VDContextID.UNSET_CONTEXT)
            {
                throw new InvalidOperationException($"Context for {typeof(TInstance)} is not set!");
            }
        }
    }
}
