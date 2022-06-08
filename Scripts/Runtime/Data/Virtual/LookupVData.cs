using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public class LookupVData<TKey, TValue> : AbstractVData
        where TKey : struct, IEquatable<TKey>
        where TValue : struct, ILookupValue<TKey>
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private const string STATE_WORK = "Work";
        private const string STATE_ADD_MAIN_THREAD = "AddMainThread";
        private const string STATE_EXTERNAL_WORK = "ExternalWork";
#endif

        private static readonly int VALUE_SIZE = UnsafeUtility.SizeOf<TValue>();
        // ReSharper disable once StaticMemberInGenericType
        private static readonly int MAIN_THREAD_INDEX = ParallelAccessUtil.CollectionIndexForMainThread();

        private UnsafeTypedStream<TValue> m_PendingAdd;
        private UnsafeTypedStream<TKey> m_PendingRemove;
        private DeferredNativeArray<TValue> m_Iteration;
        private NativeHashMap<TKey, TValue> m_Lookup;


        public LookupVData(int initialCapacity, BatchStrategy batchStrategy) : this(initialCapacity, batchStrategy, NULL_VDATA)
        {
        }

        public LookupVData(int initialCapacity, BatchStrategy batchStrategy, AbstractVData input) : base(input)
        {
            m_PendingAdd = new UnsafeTypedStream<TValue>(Allocator.Persistent,
                                                         Allocator.TempJob);
            m_PendingRemove = new UnsafeTypedStream<TKey>(Allocator.Persistent,
                                                          Allocator.TempJob);
            m_Iteration = new DeferredNativeArray<TValue>(Allocator.Persistent,
                                                          Allocator.TempJob);
            m_Lookup = new NativeHashMap<TKey, TValue>(initialCapacity, Allocator.Persistent);


            //TODO: Check on this - duplicated in VData?
            // BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
            //     ? ChunkUtil.MaxElementsPerChunk<T>()
            //     : 1;
        }

        protected override void DisposeSelf()
        {
            m_PendingAdd.Dispose();
            m_PendingRemove.Dispose();
            m_Iteration.Dispose();
            m_Lookup.Dispose();

            base.DisposeSelf();
        }
        
        public JobDataForCompletion<TValue> GetCompletionWriter()
        {
            return new JobDataForCompletion<TValue>(m_PendingAdd.AsWriter());
        }

        public JobDataForAddMT<TValue> AcquireForAdd()
        {
            ValidateAcquireState(STATE_ADD_MAIN_THREAD);
            AccessController.Acquire(AccessType.SharedWrite);
            return new JobDataForAddMT<TValue>(m_PendingAdd.AsLaneWriter(MAIN_THREAD_INDEX));
        }

        public void ReleaseForAdd()
        {
            ValidateReleaseState(STATE_ADD_MAIN_THREAD);
            AccessController.Release();
        }
        
        //TODO: Split to different functions - Consolidate and Acquires are different
        //TODO: Commonality for the work in VDATA
        public JobHandle AcquireForWork(JobHandle dependsOn, out LookupJobDataForWork<TKey, TValue> workStruct)
        {
            ValidateAcquireState(STATE_WORK);
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            ConsolidateLookupJob consolidateLookupJob = new ConsolidateLookupJob(m_PendingAdd,
                                                                                 m_PendingRemove,
                                                                                 m_Lookup,
                                                                                 m_Iteration);
            JobHandle consolidateHandle = consolidateLookupJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            workStruct = new LookupJobDataForWork<TKey, TValue>(m_PendingRemove.AsWriter(),
                                                                m_Lookup,
                                                                m_Iteration.AsDeferredJobArray());

            return AcquireOutputsAsync(consolidateHandle);
        }

        public void ReleaseForWork(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_WORK);
            AccessController.ReleaseAsync(releaseAccessDependency);
            ReleaseOutputsAsync(releaseAccessDependency);
        }

        public JobHandle AcquireForExternalWork(out LookupJobDataForExternalWork<TKey, TValue> workStruct)
        {
            ValidateAcquireState(STATE_EXTERNAL_WORK);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);

            workStruct = new LookupJobDataForExternalWork<TKey, TValue>(m_PendingRemove.AsWriter(),
                                                                        m_Lookup,
                                                                        m_Iteration.AsDeferredJobArray());

            return sharedWriteHandle;
        }

        public void ReleaseForExternalWork(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_EXTERNAL_WORK);
            AccessController.ReleaseAsync(releaseAccessDependency);
        }


        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateLookupJob : IJob
        {
            private UnsafeTypedStream<TValue> m_PendingAdd;
            private UnsafeTypedStream<TKey> m_PendingRemove;
            private NativeHashMap<TKey, TValue> m_Lookup;
            private DeferredNativeArray<TValue> m_Iteration;

            public ConsolidateLookupJob(UnsafeTypedStream<TValue> pendingAdd,
                                        UnsafeTypedStream<TKey> pendingRemove,
                                        NativeHashMap<TKey, TValue> lookup,
                                        DeferredNativeArray<TValue> iteration)
            {
                m_PendingAdd = pendingAdd;
                m_PendingRemove = pendingRemove;
                m_Lookup = lookup;
                m_Iteration = iteration;
            }

            public unsafe void Execute()
            {
                NativeArray<TKey> pendingRemoveArray = m_PendingRemove.ToNativeArray(Allocator.Temp);
                NativeArray<TValue> pendingAddArray = m_PendingAdd.ToNativeArray(Allocator.Temp);

                if (pendingRemoveArray.Length <= 0
                 && pendingAddArray.Length <= 0)
                {
                    return;
                }

                for (int i = 0; i < pendingRemoveArray.Length; ++i)
                {
                    m_Lookup.Remove(pendingRemoveArray[i]);
                }

                for (int i = 0; i < pendingAddArray.Length; ++i)
                {
                    TValue value = pendingAddArray[i];
                    m_Lookup.Add(value.Key, value);
                }

                m_PendingAdd.Clear();
                m_PendingRemove.Clear();
                m_Iteration.Clear();

                NativeArray<TValue> valuesInLookup = m_Lookup.GetValueArray(Allocator.Temp);
                NativeArray<TValue> iterationArray = m_Iteration.DeferredCreate(valuesInLookup.Length);

                void* valuesInLookupPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(valuesInLookup);
                void* iterationArrayPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(iterationArray);
                long bytes = valuesInLookup.Length * VALUE_SIZE;

                UnsafeUtility.MemCpy(iterationArrayPtr, valuesInLookupPtr, bytes);
            }
        }
    }
}
