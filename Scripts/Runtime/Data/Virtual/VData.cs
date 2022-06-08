using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public class VData<T> : AbstractVData
        where T : struct
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private const string STATE_ENTITIES_ADD = "EntitiesAdd";
        private const string STATE_ADD = "Add";
        private const string STATE_WORK = "Work";
#endif

        private UnsafeTypedStream<T> m_Pending;
        private DeferredNativeArray<T> m_Current;

        public DeferredNativeArray<T> ArrayForScheduling
        {
            get => m_Current;
        }

        public int BatchSize
        {
            get;
        }

        public VData(BatchStrategy batchStrategy) : this(batchStrategy, NULL_VDATA)
        {
        }

        public VData(BatchStrategy batchStrategy, AbstractVData input) : base(input)
        {
            m_Pending = new UnsafeTypedStream<T>(Allocator.Persistent,
                                                 Allocator.TempJob);
            m_Current = new DeferredNativeArray<T>(Allocator.Persistent,
                                                   Allocator.TempJob);

            //TODO: Check on this
            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<T>()
                : 1;
        }

        protected override void DisposeSelf()
        {
            m_Pending.Dispose();
            m_Current.Dispose();

            base.DisposeSelf();
        }


        public JobDataForCompletion<T> GetCompletionWriter()
        {
            return new JobDataForCompletion<T>(m_Pending.AsWriter());
        }

        public JobHandle AcquireForEntitiesAdd(out JobDataForEntitiesAdd<T> workStruct)
        {
            ValidateAcquireState(STATE_ENTITIES_ADD);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);

            workStruct = new JobDataForEntitiesAdd<T>(m_Pending.AsWriter());

            return sharedWriteHandle;
        }

        public void ReleaseForEntitiesAdd(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState();
            AccessController.ReleaseAsync(releaseAccessDependency);
        }


        public JobHandle AcquireForAdd(out JobDataForAdd<T> workStruct)
        {
            ValidateAcquireState(STATE_ADD);
            JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);

            workStruct = new JobDataForAdd<T>(m_Pending.AsWriter());

            return sharedWriteHandle;
        }

        public void ReleaseForAdd(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState();
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public JobHandle AcquireForWork(JobHandle dependsOn, out JobDataForWork<T> workStruct)
        {
            ValidateAcquireState(STATE_WORK);
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            //Consolidate everything in pending into current so it can be balanced across threads
            ConsolidateToNativeArrayJob<T> consolidateJob = new ConsolidateToNativeArrayJob<T>(m_Pending.AsReader(),
                                                                                               m_Current);
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            //Clear pending so we can use it again
            JobHandle clearHandle = m_Pending.Clear(consolidateHandle);

            //Create the work struct
            workStruct = new JobDataForWork<T>(m_Pending.AsWriter(),
                                               m_Current.AsDeferredJobArray());


            return AcquireOutputsAsync(clearHandle);
        }

        public void ReleaseForWork(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState();
            AccessController.ReleaseAsync(releaseAccessDependency);
            ReleaseOutputsAsync(releaseAccessDependency);
        }
    }
}
