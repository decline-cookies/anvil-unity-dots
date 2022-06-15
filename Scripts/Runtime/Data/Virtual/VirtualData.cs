using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public class VirtualData<TKey, TValue> : AbstractVirtualData
        where TKey : struct, IEquatable<TKey>
        where TValue : struct, ILookupData<TKey>
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        //TODO: Can we make this an enum
        private const string STATE_WORK = "Work";
        private const string STATE_ADD_MAIN_THREAD = "AddMainThread";
        private const string STATE_ADD = "Add";
        private const string STATE_ENTITIES_ADD = "EntitiesAdd";
#endif

        // ReSharper disable once StaticMemberInGenericType
        private static readonly int MAIN_THREAD_INDEX = ParallelAccessUtil.CollectionIndexForMainThread();

        private static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<TValue>();

        private UnsafeTypedStream<TValue> m_Pending;
        private DeferredNativeArray<TValue> m_Iteration;
        private UnsafeHashMap<TKey, TValue> m_Lookup;


        public DeferredNativeArray<TValue> ArrayForScheduling
        {
            get => m_Iteration;
        }

        public int BatchSize
        {
            get;
        }

        public VirtualData(BatchStrategy batchStrategy) : this(batchStrategy, NULL_VDATA)
        {
        }

        public VirtualData(BatchStrategy batchStrategy, AbstractVirtualData input) : base(input)
        {
            m_Pending = new UnsafeTypedStream<TValue>(Allocator.Persistent,
                                                      Allocator.TempJob);
            m_Iteration = new DeferredNativeArray<TValue>(Allocator.Persistent,
                                                          Allocator.TempJob);

            m_Lookup = new UnsafeHashMap<TKey, TValue>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);


            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? MAX_ELEMENTS_PER_CHUNK
                : 1;
        }

        protected override void DisposeSelf()
        {
            m_Pending.Dispose();
            m_Iteration.Dispose();
            m_Lookup.Dispose();

            base.DisposeSelf();
        }

        //TODO: Main Thread vs Threaded Variants

        public JobResultWriter<TValue> GetResultWriter()
        {
            return new JobResultWriter<TValue>(m_Pending.AsWriter());
        }

        public JobInstanceWriterMainThread<TValue> AcquireForAdd()
        {
            ValidateAcquireState(STATE_ADD_MAIN_THREAD);
            AccessController.Acquire(AccessType.SharedWrite);
            return new JobInstanceWriterMainThread<TValue>(m_Pending.AsLaneWriter(MAIN_THREAD_INDEX));
        }

        public void ReleaseForAdd()
        {
            ValidateReleaseState(STATE_ADD_MAIN_THREAD);
            AccessController.Release();
        }

        public JobHandle AcquireForEntitiesAddAsync(out JobInstanceWriterEntities<TValue> jobInstanceWriterDataForEntitiesAdd)
        {
            ValidateAcquireState(STATE_ENTITIES_ADD);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);
            jobInstanceWriterDataForEntitiesAdd = new JobInstanceWriterEntities<TValue>(m_Pending.AsWriter());
            return sharedWriteHandle;
        }

        public void ReleaseForEntitiesAddAsync(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_ENTITIES_ADD);
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public JobHandle AcquireForAddAsync(out JobInstanceWriter<TValue> jobInstanceForAdd)
        {
            ValidateAcquireState(STATE_ADD);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);
            jobInstanceForAdd = new JobInstanceWriter<TValue>(m_Pending.AsWriter());
            return sharedWriteHandle;
        }

        public void ReleaseForAddAsync(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_ADD);
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateLookupJob consolidateLookupJob = new ConsolidateLookupJob(m_Pending,
                                                                                 m_Iteration,
                                                                                 m_Lookup);
            JobHandle consolidateHandle = consolidateLookupJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }

        //TODO: Commonality for the work in VDATA
        public JobHandle AcquireForUpdate(out JobInstanceUpdater<TKey, TValue> workStruct)
        {
            ValidateAcquireState(STATE_WORK);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);

            workStruct = new JobInstanceUpdater<TKey, TValue>(m_Pending.AsWriter(),
                                                           m_Iteration.AsDeferredJobArray(),
                                                           m_Lookup);

            return AcquireOutputsAsync(sharedWriteHandle);
        }

        public void ReleaseForUpdate(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_WORK);
            AccessController.ReleaseAsync(releaseAccessDependency);
            ReleaseOutputsAsync(releaseAccessDependency);
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateLookupJob : IJob
        {
            private UnsafeTypedStream<TValue> m_Pending;
            private DeferredNativeArray<TValue> m_Iteration;
            private UnsafeHashMap<TKey, TValue> m_Lookup;

            public ConsolidateLookupJob(UnsafeTypedStream<TValue> pending,
                                        DeferredNativeArray<TValue> iteration,
                                        UnsafeHashMap<TKey, TValue> lookup)
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_Lookup = lookup;
            }

            public void Execute()
            {
                m_Lookup.Clear();
                m_Iteration.Clear();

                NativeArray<TValue> iterationArray = m_Iteration.DeferredCreate(m_Pending.Count());
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();

                for (int i = 0; i < iterationArray.Length; ++i)
                {
                    TValue value = iterationArray[i];
                    m_Lookup.TryAdd(value.Key, value);
                }
            }
        }
    }
}
