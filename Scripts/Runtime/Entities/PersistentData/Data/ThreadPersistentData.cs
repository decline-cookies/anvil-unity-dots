using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities.TaskDriver;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class ThreadPersistentData<T> : AbstractTypedPersistentData<UnsafeArray<T>>,
                                             IThreadPersistentData<T>
        where T : unmanaged, IThreadPersistentDataInstance
    {
        public ThreadPersistentData()
            : base(new UnsafeArray<T>(ParallelAccessUtil.CollectionSizeForMaxThreads, Allocator.Persistent))
        {
            ref UnsafeArray<T> data = ref Data;
            for (int i = 0; i < data.Length; ++i)
            {
                T instance = new T();
                instance.ConstructForThread(i);
                data[i] = instance;
            }
        }

        protected override void DisposeData()
        {
            ref UnsafeArray<T> data = ref Data;
            for (int i = 0; i < data.Length; ++i)
            {
                data[i].DisposeForThread(i);
            }

            base.DisposeData();
        }

        public ThreadPersistentDataAccessor<T> CreateThreadPersistentDataAccessor()
        {
            return new ThreadPersistentDataAccessor<T>(ref Data);
        }

        /// <inheritdoc cref="IThreadPersistentData{T}.AcquireAsync"/>
        public JobHandle AcquireAsync(out ThreadPersistentDataAccessor<T> accessor)
        {
            //Thread data is only for one thread at a time so we can assume shared write.
            JobHandle dependsOn = AcquireAsync(AccessType.SharedWrite);
            accessor = CreateThreadPersistentDataAccessor();
            return dependsOn;
        }

        /// <inheritdoc cref="IThreadPersistentData{T}.Acquire"/>
        public ThreadPersistentDataAccessor<T> Acquire()
        {
            //Thread data is only for one thread at a time so we can assume shared write.
            Acquire(AccessType.SharedWrite);
            return CreateThreadPersistentDataAccessor();
        }
    }
}
