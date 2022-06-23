using Anvil.Unity.DOTS.Data;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    public class MainThreadTaskWorkConfig : AbstractTaskWorkConfig
    {
        private readonly MainThreadTaskWorkData m_MainThreadTaskWorkData;

        internal MainThreadTaskWorkConfig(AbstractTaskDriverSystem abstractTaskDriverSystem)
        {
            m_MainThreadTaskWorkData = new MainThreadTaskWorkData(abstractTaskDriverSystem);
        }

        public MainThreadTaskWorkData Acquire()
        {
            foreach (IDataWrapper wrapper in DataWrappers)
            {
                wrapper.Acquire();
            }
            
            return m_MainThreadTaskWorkData;
        }

        internal void Release()
        {
            foreach (IDataWrapper wrapper in DataWrappers)
            {
                wrapper.Release(default);
            }
        }
        
        public MainThreadTaskWorkConfig RequireDataForAdd<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForAdd wrapper = new VDWrapperForAdd(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }
        
        public MainThreadTaskWorkConfig RequireDataForAdd<TKey, TInstance, TResult>(VirtualData<TKey, TInstance> data, VirtualData<TKey, TResult> resultsDestination)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
            where TResult : struct, IKeyedData<TKey>
        {
            RequireDataForAdd(data);
            RequireDataAsResultsDestination(resultsDestination);
            return this;
        }
        
        public MainThreadTaskWorkConfig RequireDataForIterate<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForIterate wrapper = new VDWrapperForIterate(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }
        
        public MainThreadTaskWorkConfig RequireDataForUpdate<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForUpdate wrapper = new VDWrapperForUpdate(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }

        public MainThreadTaskWorkConfig RequireDataAsResultsDestination<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperAsResultsDestination wrapper = new VDWrapperAsResultsDestination(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }
    }
}
