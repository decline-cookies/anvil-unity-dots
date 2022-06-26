using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskWorkData
    {
        private readonly Dictionary<Type, IDataWrapper> m_WrappedDataLookup;
        
        public AbstractTaskDriverSystem System
        {
            get;
        }

        public World World
        {
            get;
        }

        public ref readonly TimeData Time
        {
            get => ref World.Time;
        }
        
        protected AbstractTaskWorkData(AbstractTaskDriverSystem system)
        {
            System = system;
            World = System.World;
            m_WrappedDataLookup = new Dictionary<Type, IDataWrapper>();
        }
        
        internal void AddDataWrapper(Type type, IDataWrapper dataWrapper)
        {
            m_WrappedDataLookup.Add(type, dataWrapper);
        }
        
        protected VirtualData<TKey, TInstance> GetVirtualData<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            //TODO: Exceptions
            IDataWrapper wrapper = m_WrappedDataLookup[typeof(VirtualData<TKey, TInstance>)];
            return (VirtualData<TKey, TInstance>)wrapper.Data;
        }
        
        public VDReader<TInstance> GetVDReader<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>(); 
            VDReader<TInstance> reader = virtualData.CreateVDReader();
            return reader;
        }
        
        public VDResultsDestination<TResult> GetVDResultsDestination<TKey, TResult>()
            where TKey : unmanaged, IEquatable<TKey>
            where TResult : unmanaged, IKeyedData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TResult> virtualData = GetVirtualData<TKey, TResult>(); 
            VDResultsDestination<TResult> resultsDestination = virtualData.CreateVDResultsDestination();
            return resultsDestination;
        }

        public virtual VDUpdater<TKey, TInstance> GetVDUpdater<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>();
            VDUpdater<TKey, TInstance> updater = virtualData.CreateVDUpdater();
            return updater;
        }

        public virtual VDWriter<TInstance> GetVDWriter<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>(); 
            VDWriter<TInstance> writer = virtualData.CreateVDWriter();
            return writer;
        }
    }
}
