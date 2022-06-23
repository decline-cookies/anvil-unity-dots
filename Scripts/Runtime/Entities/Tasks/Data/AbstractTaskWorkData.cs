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
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            //TODO: Exceptions
            IDataWrapper wrapper = m_WrappedDataLookup[typeof(VirtualData<TKey, TInstance>)];
            return (VirtualData<TKey, TInstance>)wrapper.Data;
        }

        public abstract VDUpdater<TKey, TInstance> GetVDUpdater<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>;

        public abstract VDReader<TInstance> GetVDReader<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>;

        public abstract VDWriter<TInstance> GetVDWriter<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>;

        public abstract VDResultsDestination<TResult> GetVDResultsDestination<TKey, TResult>()
            where TKey : struct, IEquatable<TKey>
            where TResult : struct, IKeyedData<TKey>;
    }
}
