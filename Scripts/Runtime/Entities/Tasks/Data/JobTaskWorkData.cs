using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobTaskWorkData : AbstractTaskWorkData
    {
        internal JobTaskWorkData(AbstractTaskDriverSystem system) : base(system)
        {
        }

        public sealed override VDUpdater<TKey, TInstance> GetVDUpdater<TKey, TInstance>()
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>();
            VDUpdater<TKey, TInstance> updater = virtualData.CreateVDUpdater();
            return updater;
        }

        public sealed override VDReader<TInstance> GetVDReader<TKey, TInstance>()
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>(); 
            VDReader<TInstance> reader = virtualData.CreateVDReader();
            return reader;
        }

        public sealed override VDWriter<TInstance> GetVDWriter<TKey, TInstance>()
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>(); 
            VDWriter<TInstance> writer = virtualData.CreateVDWriter();
            return writer;
        }

        public sealed override VDResultsDestination<TResult> GetVDResultsDestination<TKey, TResult>()
        {
            //TODO: Exceptions
            VirtualData<TKey, TResult> virtualData = GetVirtualData<TKey, TResult>(); 
            VDResultsDestination<TResult> resultsDestination = virtualData.CreateVDResultsDestination();
            return resultsDestination;
        }
    }
}
