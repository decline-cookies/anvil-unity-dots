using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public abstract class AbstractVirtualData<T> : AbstractAnvilBase
        where T : struct
    {

        protected AccessController AccessController
        {
            get;
        }

        protected UnsafeTypedStream<T> Pending
        {
            get;
        }

        protected DeferredNativeArray<T> Current
        {
            get;
        }

        protected AbstractVirtualData()
        {
            AccessController = new AccessController();
            Pending = new UnsafeTypedStream<T>(Allocator.Persistent, Allocator.TempJob);
            Current = new DeferredNativeArray<T>(Allocator.Persistent, Allocator.TempJob);

            //TODO: Allow for better batching rules - Spread evenly across X threads, maximizing chunk
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

        //TODO: Balanced batch across X threads
        public int BatchSize
        {
            get;
        }

        //*************************************************************************************************************
        // IDataOwner
        //*************************************************************************************************************

        protected virtual JobHandle InternalAcquireProcessorAsync(JobHandle dependsOn)
        {
            //Get access to be able to write exclusively, we need to update everything
            JobHandle exclusiveWrite = AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            //Consolidate everything in pending into current so it can be balanced across threads
            ConsolidateToNativeArrayJob<T> consolidateJob = new ConsolidateToNativeArrayJob<T>(Pending.AsReader(),
                                                                                               Current);
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWrite));

            //Clear pending so we can use it again
            JobHandle clearHandle = Pending.Clear(consolidateHandle);

            return clearHandle;
        }

        public virtual void ReleaseProcessorAsync(JobHandle releaseAccessDependency)
        {
            //The native array of current values has been read from this frame, we can clear it.
            JobHandle clearHandle = Current.Clear(releaseAccessDependency);
            //Others can use this again
            AccessController.ReleaseAsync(JobHandle.CombineDependencies(releaseAccessDependency, clearHandle));
        }


        protected UnsafeTypedStream<T>.Writer AcquirePending(AccessType accessType)
        {
            //TODO: Collections Checks
            AccessController.Acquire(accessType);
            return Pending.AsWriter();
        }

        protected JobHandle AcquirePendingAsync(AccessType accessType, out UnsafeTypedStream<T>.Writer pendingWriter)
        {
            //TODO: Collections Checks
            pendingWriter = Pending.AsWriter();
            return AccessController.AcquireAsync(accessType);
        }

        protected void ReleasePending()
        {
            //TODO: Collections Checks
            AccessController.Release();
        }

        protected void ReleasePendingAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections Checks
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        protected JobHandle AcquireAllAsync(AccessType accessType, out UnsafeTypedStream<T>.Writer pendingWriter, out NativeArray<T> current)
        {
            //TODO: Collections Checks
            pendingWriter = Pending.AsWriter();
            current = Current.AsDeferredJobArray();
            return AccessController.AcquireAsync(accessType);
        }

        protected void ReleaseAllAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections Checks
            AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
