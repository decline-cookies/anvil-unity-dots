using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public class VirtualData<TKey, TInstance> : AbstractVirtualData
        where TKey : struct, IEquatable<TKey>
        where TInstance : struct, ILookupData<TKey>
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        //TODO: Can we make this an enum
        private const string STATE_WORK = "Work";
        private const string STATE_ADD_MAIN_THREAD = "AddMainThread";
        private const string STATE_ADD = "Add";
        private const string STATE_READ = "Read";
        private const string STATE_ENTITIES_ADD = "EntitiesAdd";
#endif
        
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<TInstance>();
        
        // ReSharper disable once StaticMemberInGenericType
        private static readonly int MAIN_THREAD_INDEX = ParallelAccessUtil.CollectionIndexForMainThread();

        private UnsafeTypedStream<TInstance> m_Pending;
        private DeferredNativeArray<TInstance> m_Iteration;
        private UnsafeHashMap<TKey, TInstance> m_Lookup;


        public DeferredNativeArray<TInstance> ArrayForScheduling
        {
            get => m_Iteration;
        }

        public VirtualData(AbstractVirtualData input = null) : base(input)
        {
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

            base.DisposeSelf();
        }

        //TODO: Main Thread vs Threaded Variants

        public JobResultWriter<TInstance> GetJobResultWriter()
        {
            //TODO: Exceptions
            return new JobResultWriter<TInstance>(m_Pending.AsWriter());
        }

        public JobInstanceUpdater<TKey, TInstance> GetJobInstanceUpdater()
        {
            //TODO: Exceptions
            return new JobInstanceUpdater<TKey, TInstance>(m_Pending.AsWriter(),
                                                           m_Iteration.AsDeferredJobArray(),
                                                           m_Lookup);
        }
        
        public JobInstanceReader<TInstance> GetJobInstanceReader()
        {
            //TODO: Exceptions
            return new JobInstanceReader<TInstance>(m_Iteration.AsDeferredJobArray());
        }
        
        public JobInstanceWriter<TInstance> GetJobInstanceWriter()
        {
            //TODO: Exceptions
            return new JobInstanceWriter<TInstance>(m_Pending.AsWriter());
        }
        
        public JobInstanceWriterEntities<TInstance> GetJobInstanceWriterEntities()
        {
            //TODO: Exceptions
            return new JobInstanceWriterEntities<TInstance>(m_Pending.AsWriter());
        }

        public JobInstanceWriterMainThread<TInstance> AcquireForAdd()
        {
            ValidateAcquireState(STATE_ADD_MAIN_THREAD);
            AccessController.Acquire(AccessType.SharedWrite);
            return new JobInstanceWriterMainThread<TInstance>(m_Pending.AsLaneWriter(MAIN_THREAD_INDEX));
        }

        public void ReleaseForAdd()
        {
            ValidateReleaseState(STATE_ADD_MAIN_THREAD);
            AccessController.Release();
        }

        public JobHandle AcquireForEntitiesAddAsync(out JobInstanceWriterEntities<TInstance> jobInstanceWriterDataForEntitiesAdd)
        {
            ValidateAcquireState(STATE_ENTITIES_ADD);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);
            jobInstanceWriterDataForEntitiesAdd = new JobInstanceWriterEntities<TInstance>(m_Pending.AsWriter());
            return sharedWriteHandle;
        }

        public void ReleaseForEntitiesAddAsync(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_ENTITIES_ADD);
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public JobHandle AcquireForAddAsync(out JobInstanceWriter<TInstance> jobInstanceForAdd)
        {
            ValidateAcquireState(STATE_ADD);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);
            jobInstanceForAdd = new JobInstanceWriter<TInstance>(m_Pending.AsWriter());
            return sharedWriteHandle;
        }

        public void ReleaseForAddAsync(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_ADD);
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public JobHandle AcquireForReadAsync(out JobInstanceReader<TInstance> jobInstanceReader)
        {
            ValidateAcquireState(STATE_READ);
            JobHandle sharedReadHandle = AccessController.AcquireAsync(AccessType.SharedRead);
            jobInstanceReader = new JobInstanceReader<TInstance>(m_Iteration.AsDeferredJobArray());
            return sharedReadHandle;
        }


        public void ReleaseForReadAsync(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_READ);
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
        public JobHandle AcquireForUpdate(out JobInstanceUpdater<TKey, TInstance> jobInstanceUpdater)
        {
            ValidateAcquireState(STATE_WORK);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);

            jobInstanceUpdater = new JobInstanceUpdater<TKey, TInstance>(m_Pending.AsWriter(),
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
