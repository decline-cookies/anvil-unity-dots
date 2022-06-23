using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class MainThreadTaskWorkData : AbstractTaskWorkData
    {
        private static readonly int MAIN_THREAD_INDEX = ParallelAccessUtil.CollectionIndexForMainThread();
        
        internal MainThreadTaskWorkData(AbstractTaskDriverSystem system) : base(system)
        {
        }

        public sealed override VDUpdater<TKey, TInstance> GetVDUpdater<TKey, TInstance>()
        {
            VDUpdater<TKey, TInstance> updater = base.GetVDUpdater<TKey, TInstance>();
            updater.InitForThread(MAIN_THREAD_INDEX);
            return updater;
        }

        public sealed override VDWriter<TInstance> GetVDWriter<TKey, TInstance>()
        {
            VDWriter<TInstance> writer = base.GetVDWriter<TKey, TInstance>();
            writer.InitForThread(MAIN_THREAD_INDEX);
            return writer;
        }
    }
}
