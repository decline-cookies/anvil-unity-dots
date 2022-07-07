using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Job information object to aid with scheduling and populating a job instance.
    /// Data acquired from this object is guaranteed to have the proper access.
    /// </summary>
    public class TaskWorkData<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        //We don't have to be on the main thread, but it makes sense as a good default
        // ReSharper disable once StaticMemberInGenericType
        private static readonly int SYNCHRONOUS_THREAD_INDEX = ParallelAccessUtil.CollectionIndexForMainThread();
        
        private readonly Dictionary<Type, AbstractVDWrapper<TKey>> m_WrappedDataLookup;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly Dictionary<Type, AbstractTaskWorkConfig<TKey>.DataUsage> m_DataUsageByType;
#endif
        
        /// <summary>
        /// The <see cref="AbstractTaskDriverSystem"/> this job is being scheduled during.
        /// Calls to <see cref="SystemBase.GetComponentDataFromEntity{T}"/> and similar will attribute dependencies
        /// correctly.
        /// </summary>
        public AbstractTaskDriverSystem<TKey> System
        {
            get;
        }
        
        /// <summary>
        /// The <see cref="World"/> this job is being scheduled under.
        /// </summary>
        public World World
        {
            get;
        }
        
        /// <summary>
        /// Helper function for accessing the <see cref="TimeData"/> normally found on <see cref="SystemBase.Time"/>
        /// </summary>
        public ref readonly TimeData Time
        {
            get => ref World.Time;
        }

        internal TaskWorkData(AbstractTaskDriverSystem<TKey> system)
        {
            System = system;
            World = System.World;
            m_WrappedDataLookup = new Dictionary<Type, AbstractVDWrapper<TKey>>();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_DataUsageByType = new Dictionary<Type, AbstractTaskWorkConfig<TKey>.DataUsage>();
#endif
        }

        internal void AddDataWrapper(AbstractVDWrapper<TKey> dataWrapper)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_WrappedDataLookup.ContainsKey(dataWrapper.Type))
            {
                throw new InvalidOperationException($"{this} already contains data registered for {dataWrapper.Type}. Please ensure that data is not registered more than once.");
            }
#endif
            m_WrappedDataLookup.Add(dataWrapper.Type, dataWrapper);
        }

        private VirtualData<TKey, TInstance> GetVirtualData<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            Type type = typeof(VirtualData<TKey, TInstance>);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_WrappedDataLookup.ContainsKey(type))
            {
                throw new InvalidOperationException($"Tried to get {nameof(VirtualData<TKey, TInstance>)} but it doesn't exist on {this}. Please ensure a \"RequireData\" function was called on the corresponding config.");
            }
#endif
            AbstractVDWrapper<TKey> wrapper = m_WrappedDataLookup[type];
            return (VirtualData<TKey, TInstance>)wrapper.Data;
        }

        private CancelVirtualData<TKey> GetCancelData()
        {
            Type type = typeof(CancelVirtualData<TKey>);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_WrappedDataLookup.ContainsKey(type))
            {
                throw new InvalidOperationException($"Tried to get {nameof(CancelVirtualData<TKey>)} but it doesn't exist on {this}. Please ensure a \"RequireData\" function was called on the corresponding config.");
            }
#endif
            AbstractVDWrapper<TKey> wrapper = m_WrappedDataLookup[type];
            return (CancelVirtualData<TKey>)wrapper.Data;
        }

        /// <summary>
        /// Returns a <see cref="VDReader{TInstance}"/> for use in a job.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>The <see cref="VDReader{TInstance}"/></returns>
        public VDReader<TInstance> GetVDReaderAsync<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.IterateAsync);
#endif
            
            VDReader<TInstance> reader = virtualData.CreateVDReader();
            return reader;
        }
        
        /// <summary>
        /// Returns a <see cref="VDReader{TInstance}"/> for synchronous use.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>The <see cref="VDReader{TInstance}"/></returns>
        public VDReader<TInstance> GetVDReader<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.Iterate);
#endif
            
            VDReader<TInstance> reader = virtualData.CreateVDReader();
            return reader;
        }

        /// <summary>
        /// Returns a <see cref="VDResultsDestination{TResult}"/> for use in a job.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TResult">The type of the data</typeparam>
        /// <returns>The <see cref="VDResultsDestination{TResult}"/></returns>
        public VDResultsDestination<TResult> GetVDResultsDestinationAsync<TResult>()
            where TResult : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TResult> virtualData = GetVirtualData<TResult>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.ResultsDestinationAsync);
#endif
            
            VDResultsDestination<TResult> resultsDestination = virtualData.CreateVDResultsDestination();
            return resultsDestination;
        }
        
        /// <summary>
        /// Returns a <see cref="VDResultsDestination{TResult}"/> for synchronous use.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TResult">The type of the data</typeparam>
        /// <returns>The <see cref="VDResultsDestination{TResult}"/></returns>
        public VDResultsDestination<TResult> GetVDResultsDestination<TResult>()
            where TResult : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TResult> virtualData = GetVirtualData<TResult>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.ResultsDestination);
#endif
            
            VDResultsDestination<TResult> resultsDestination = virtualData.CreateVDResultsDestination();
            return resultsDestination;
        }
        
        /// <summary>
        /// Returns a <see cref="VDUpdater{TKey, TInstance}"/> for use in a job.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>The <see cref="VDUpdater{TKey, TInstance}"/></returns>
        public VDUpdater<TKey, TInstance> GetVDUpdaterAsync<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.UpdateAsync);
#endif
            
            VDUpdater<TKey, TInstance> updater = virtualData.CreateVDUpdater();
            return updater;
        }
        
        /// <summary>
        /// Returns a <see cref="VDUpdater{TKey, TInstance}"/> for synchronous use.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>The <see cref="VDUpdater{TKey, TInstance}"/></returns>
        public VDUpdater<TKey, TInstance> GetVDUpdater<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.Update);
#endif
            
            VDUpdater<TKey, TInstance> updater = virtualData.CreateVDUpdater();
            updater.InitForThread(SYNCHRONOUS_THREAD_INDEX);
            return updater;
        }
        
        /// <summary>
        /// Returns a <see cref="VDWriter{TInstance}"/> for use in a job.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>The <see cref="VDWriter{TInstance}"/></returns>
        public VDWriter<TInstance> GetVDWriterAsync<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.AddAsync);
#endif
            
            VDWriter<TInstance> writer = virtualData.CreateVDWriter();
            return writer;
        }
        
        /// <summary>
        /// Returns a <see cref="VDWriter{TInstance}"/> for synchronous use.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>The <see cref="VDWriter{TInstance}"/></returns>
        public VDWriter<TInstance> GetVDWriter<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.Add);
#endif
            
            VDWriter<TInstance> writer = virtualData.CreateVDWriter();
            writer.InitForThread(SYNCHRONOUS_THREAD_INDEX);
            return writer;
        }

        public VDCancelWriter<TKey> GetVDCancelWriterAsync()
        {
            CancelVirtualData<TKey> cancelData = GetCancelData();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(cancelData.Type, AbstractTaskWorkConfig<TKey>.DataUsage.RequestCancelAsync);
#endif

            VDCancelWriter<TKey> cancelWriter = cancelData.CreateVDCancelWriter();
            return cancelWriter;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal void Debug_NotifyWorkDataOfUsage(Type type, AbstractTaskWorkConfig<TKey>.DataUsage usage)
        {
            m_DataUsageByType.Add(type, usage);
        }

        private void CheckUsage(Type type, AbstractTaskWorkConfig<TKey>.DataUsage expectedUsage)
        {
            AbstractTaskWorkConfig<TKey>.DataUsage dataUsage = m_DataUsageByType[type];
            if (dataUsage != expectedUsage)
            {
                throw new InvalidOperationException($"Trying to get data of {type} with usage of {expectedUsage} but data was required with {dataUsage}. Check the configuration for the right \"Require\" calls.");
            }
        }
#endif
    }
}
