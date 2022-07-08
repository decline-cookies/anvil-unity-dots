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
    public class TaskWorkData
    {
        //We don't have to be on the main thread, but it makes sense as a good default
        // ReSharper disable once StaticMemberInGenericType
        private static readonly int SYNCHRONOUS_THREAD_INDEX = ParallelAccessUtil.CollectionIndexForMainThread();
        
        private readonly Dictionary<Type, AbstractVDWrapper> m_WrappedDataLookup;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly Dictionary<Type, AbstractTaskWorkConfig.DataUsage> m_DataUsageByType;
#endif
        
        /// <summary>
        /// The <see cref="AbstractTaskDriverSystem"/> this job is being scheduled during.
        /// Calls to <see cref="SystemBase.GetComponentDataFromEntity{T}"/> and similar will attribute dependencies
        /// correctly.
        /// </summary>
        public AbstractTaskDriverSystem System
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

        internal TaskWorkData(AbstractTaskDriverSystem system)
        {
            System = system;
            World = System.World;
            m_WrappedDataLookup = new Dictionary<Type, AbstractVDWrapper>();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_DataUsageByType = new Dictionary<Type, AbstractTaskWorkConfig.DataUsage>();
#endif
        }

        internal void AddDataWrapper(AbstractVDWrapper dataWrapper)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_WrappedDataLookup.ContainsKey(dataWrapper.Type))
            {
                throw new InvalidOperationException($"{this} already contains data registered for {dataWrapper.Type}. Please ensure that data is not registered more than once.");
            }
#endif
            m_WrappedDataLookup.Add(dataWrapper.Type, dataWrapper);
        }

        private VirtualData<TInstance> GetVirtualData<TInstance>()
            where TInstance : unmanaged, IKeyedData
        {
            Type type = typeof(VirtualData<TInstance>);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_WrappedDataLookup.ContainsKey(type))
            {
                throw new InvalidOperationException($"Tried to get {nameof(VirtualData<TInstance>)} but it doesn't exist on {this}. Please ensure a \"RequireData\" function was called on the corresponding config.");
            }
#endif
            AbstractVDWrapper wrapper = m_WrappedDataLookup[type];
            return (VirtualData<TInstance>)wrapper.Data;
        }

        private CancelVirtualData GetCancelData()
        {
            //TODO: Type optimizations - static
            Type type = typeof(CancelVirtualData);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_WrappedDataLookup.ContainsKey(type))
            {
                throw new InvalidOperationException($"Tried to get {nameof(CancelVirtualData)} but it doesn't exist on {this}. Please ensure a \"RequireData\" function was called on the corresponding config.");
            }
#endif
            AbstractVDWrapper wrapper = m_WrappedDataLookup[type];
            return (CancelVirtualData)wrapper.Data;
        }

        /// <summary>
        /// Returns a <see cref="VDReader{TInstance}"/> for use in a job.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>The <see cref="VDReader{TInstance}"/></returns>
        public VDReader<TInstance> GetVDReaderAsync<TInstance>()
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.IterateAsync);
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
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.Iterate);
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
            where TResult : unmanaged, IKeyedData
        {
            VirtualData<TResult> virtualData = GetVirtualData<TResult>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.ResultsDestinationAsync);
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
            where TResult : unmanaged, IKeyedData
        {
            VirtualData<TResult> virtualData = GetVirtualData<TResult>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.ResultsDestination);
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
        public VDUpdater<TInstance> GetVDUpdaterAsync<TInstance>()
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.UpdateAsync);
#endif
            
            VDUpdater<TInstance> updater = virtualData.CreateVDUpdater();
            return updater;
        }
        
        /// <summary>
        /// Returns a <see cref="VDUpdater{TKey, TInstance}"/> for synchronous use.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>The <see cref="VDUpdater{TKey, TInstance}"/></returns>
        public VDUpdater<TInstance> GetVDUpdater<TInstance>()
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.Update);
#endif
            
            VDUpdater<TInstance> updater = virtualData.CreateVDUpdater();
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
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.AddAsync);
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
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = GetVirtualData<TInstance>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(virtualData.Type, AbstractTaskWorkConfig.DataUsage.Add);
#endif
            
            VDWriter<TInstance> writer = virtualData.CreateVDWriter();
            writer.InitForThread(SYNCHRONOUS_THREAD_INDEX);
            return writer;
        }

        public VDCancelWriter GetVDCancelWriterAsync()
        {
            CancelVirtualData cancelData = GetCancelData();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckUsage(cancelData.Type, AbstractTaskWorkConfig.DataUsage.RequestCancelAsync);
#endif

            VDCancelWriter cancelWriter = cancelData.CreateVDCancelWriter();
            return cancelWriter;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal void Debug_NotifyWorkDataOfUsage(Type type, AbstractTaskWorkConfig.DataUsage usage)
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
