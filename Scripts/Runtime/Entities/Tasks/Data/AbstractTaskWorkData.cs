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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly Dictionary<Type, AbstractTaskWorkConfig.DataUsage> m_DataUsageByType;
#endif

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_DataUsageByType = new Dictionary<Type, AbstractTaskWorkConfig.DataUsage>();
#endif
        }

        internal void AddDataWrapper(IDataWrapper dataWrapper)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_WrappedDataLookup.ContainsKey(dataWrapper.Type))
            {
                throw new InvalidOperationException($"{this} already contains data registered for {dataWrapper.Type}. Please ensure that data is not registered more than once.");
            }
#endif
            m_WrappedDataLookup.Add(dataWrapper.Type, dataWrapper);
        }

        private VirtualData<TKey, TInstance> GetVirtualData<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            Type type = typeof(VirtualData<TKey, TInstance>);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_WrappedDataLookup.ContainsKey(type))
            {
                throw new InvalidOperationException($"Tried to get {nameof(VirtualData<TKey, TInstance>)} but it doesn't exist on {this}. Please ensure a \"RequireData\" function was called on the corresponding config.");
            }
#endif
            IDataWrapper wrapper = m_WrappedDataLookup[type];
            return (VirtualData<TKey, TInstance>)wrapper.Data;
        }

        public VDReader<TInstance> GetVDReader<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.Iterate);
#endif
            
            VDReader<TInstance> reader = virtualData.CreateVDReader();
            return reader;
        }

        public VDResultsDestination<TResult> GetVDResultsDestination<TKey, TResult>()
            where TKey : unmanaged, IEquatable<TKey>
            where TResult : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TResult> virtualData = GetVirtualData<TKey, TResult>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.ResultsDestination);
#endif
            
            VDResultsDestination<TResult> resultsDestination = virtualData.CreateVDResultsDestination();
            return resultsDestination;
        }

        public virtual VDUpdater<TKey, TInstance> GetVDUpdater<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.Update);
#endif
            
            VDUpdater<TKey, TInstance> updater = virtualData.CreateVDUpdater();
            return updater;
        }

        public virtual VDWriter<TInstance> GetVDWriter<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TKey, TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.Add);
#endif
            
            VDWriter<TInstance> writer = virtualData.CreateVDWriter();
            return writer;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal void DebugNotifyWorkDataOfUsage(Type type, AbstractTaskWorkConfig.DataUsage usage)
        {
            m_DataUsageByType.Add(type, usage);
        }

        private void CheckUsage(Type type, AbstractTaskWorkConfig.DataUsage expectedUsage)
        {
            AbstractTaskWorkConfig.DataUsage dataUsage = m_DataUsageByType[type];
            if (dataUsage != expectedUsage)
            {
                throw new InvalidOperationException($"Trying to get data of {type} with usage of {expectedUsage} but data was required with {dataUsage}. Check the configuration for the right \"Require\" calls.");
            }
        }
#endif
    }
}
