using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractSystemData<T> : AbstractAnvilBase
        where T : struct
    {
        protected AccessController AccessController
        {
            get;
            private set;
        }

        protected UnsafeTypedStream<T> Pending
        {
            get;
            private set;
        }

        protected DeferredNativeArray<T> Current
        {
            get;
            private set;
        }

        protected AbstractSystemData()
        {
            AccessController = new AccessController();
            Pending = new UnsafeTypedStream<T>(Allocator.Persistent, Allocator.TempJob);

            BatchSize = ChunkUtil.MaxElementsPerChunk<T>();
        }

        protected override void DisposeSelf()
        {
            AccessController.Dispose();
            Pending.Dispose();
            Current.Dispose();

            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // Scheduling
        //*************************************************************************************************************

        public DeferredNativeArray<T> ArrayForScheduling
        {
            get => Current;
        }

        public int BatchSize
        {
            get;
        }


        //*************************************************************************************************************
        // IDataOwner
        //*************************************************************************************************************

        protected JobHandle AcquireForUpdate(JobHandle dependsOn)
        {
            //Get access to be able to write exclusively, we need to update everything
            JobHandle exclusiveWrite = AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            //Create a new DeferredNativeArray to hold everything we need this frame
            //TODO: Investigate reusing a DeferredNativeArray
            Current = new DeferredNativeArray<T>(Allocator.TempJob);

            //Consolidate everything in pending into current so it can be balanced across threads
            ConsolidateToNativeArrayJob<T> consolidateJob = new ConsolidateToNativeArrayJob<T>(Pending.AsReader(),
                                                                                               Current);
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWrite));

            //Clear pending so we can use it again
            JobHandle clearHandle = Pending.Clear(consolidateHandle);
            
            //If we have any channels that we might be writing responses out to, we need make sure we get access to them
            return AcquireResponseChannelsForUpdate(clearHandle);
        }

        protected virtual JobHandle AcquireResponseChannelsForUpdate(JobHandle dependsOn)
        {
            return dependsOn;
        }

        public void ReleaseForUpdate(JobHandle releaseAccessDependency)
        {
            //The native array of current values has been read from this frame, we can dispose it.
            //TODO: Look at clearing instead.
            Current.Dispose(releaseAccessDependency);
            //Others can use this again
            AccessController.ReleaseAsync(releaseAccessDependency);
            //Release all response channels as well
            ReleaseResponseChannelsForUpdate(releaseAccessDependency);
        }

        protected virtual void ReleaseResponseChannelsForUpdate(JobHandle releaseAccessDependency)
        {
        }
    }
}
