using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities.Tasks;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    internal class ThreadPersistentData<T> : AbstractTypedPersistentData<UnsafeArray<T>>,
                                             IThreadPersistentData<T>
        where T : unmanaged
    {
        private readonly IThreadPersistentData<T>.DisposalCallbackPerThread m_DisposalCallbackPerThread;
        public ThreadPersistentData(string id, 
                                    IThreadPersistentData<T>.ConstructionCallbackPerThread constructionCallbackPerThread, 
                                    IThreadPersistentData<T>.DisposalCallbackPerThread disposalCallbackPerThread) 
            : base(id, 
                   new UnsafeArray<T>(ParallelAccessUtil.CollectionSizeForMaxThreads, Allocator.Persistent))
        {
            m_DisposalCallbackPerThread = disposalCallbackPerThread;
            ref UnsafeArray<T> data = ref Data; 
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = constructionCallbackPerThread(i);
            }
        }

        protected override void DisposeData()
        {
            if (m_DisposalCallbackPerThread != null)
            {
                ref UnsafeArray<T> data = ref Data; 
                for (int i = 0; i < data.Length; ++i)
                {
                    m_DisposalCallbackPerThread(i, data[i]);
                }
            }
            base.DisposeData();
        }

        public ThreadPersistentDataAccessor<T> CreateThreadPersistentDataAccessor()
        {
            return new ThreadPersistentDataAccessor<T>(ref Data);
        }
    }
}
