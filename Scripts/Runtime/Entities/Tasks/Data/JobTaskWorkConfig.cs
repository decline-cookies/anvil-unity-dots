using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A <see cref="AbstractTaskWorkConfig"/> specific for Jobs
    /// </summary>
    public class JobTaskWorkConfig : AbstractTaskWorkConfig
    {
        /// <summary>
        /// The scheduling callback that is called when the job struct needs to be created and run through the job scheduler.
        /// </summary>
        public delegate JobHandle JobDataDelegate(JobHandle dependsOn, JobTaskWorkData jobTaskWorkData, IScheduleInfo scheduleInfo);
        
        private readonly JobDataDelegate m_JobDataDelegate;
        private readonly JobTaskWorkData m_JobTaskWorkData;
        private IScheduleInfo m_ScheduleInfo;

        internal JobTaskWorkConfig(JobDataDelegate jobDataDelegate, AbstractTaskDriverSystem abstractTaskDriverSystem)
        {
            m_JobDataDelegate = jobDataDelegate;
            m_JobTaskWorkData = new JobTaskWorkData(abstractTaskDriverSystem);
            SetTaskWorkData(m_JobTaskWorkData);
        }
        
        /// <summary>
        /// Specifies an instance of <see cref="VirtualData{TKey,TInstance}"/> to use for scheduling.
        /// This will calculate the batch size based on the <see cref="BatchStrategy"/> and size of
        /// the <typeparamref name="TInstance"/> data.
        /// NOTE: This does not ensure any access on the <see cref="VirtualData{TKey,TInstance}"/> at the time the job
        /// is actually scheduled. Please ensure that a Require function is also called for the access needed.
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> to schedule on.</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to calculate scheduling with.</param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
        public JobTaskWorkConfig ScheduleOn<TKey, TInstance>(VirtualData<TKey, TInstance> data, BatchStrategy batchStrategy)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            m_ScheduleInfo = new VirtualDataScheduleInfo<TKey, TInstance>(data, batchStrategy);
            return this;
        }
        
        /// <summary>
        /// Specifies an instance of <see cref="NativeArray{T}"/> to use for scheduling.
        /// This will calculate the batch size based on <see cref="BatchStrategy"/> and size of the
        /// <typeparamref name="T"/> data.
        /// NOTE: Access to the <see cref="NativeArray{T}"/> must be handled by the developer.
        /// </summary>
        /// <param name="array">The <see cref="NativeArray{T}"/> to schedule on.</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to calculate scheduling with.</param>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
        public JobTaskWorkConfig ScheduleOn<T>(NativeArray<T> array, BatchStrategy batchStrategy)
            where T : unmanaged
        {
            m_ScheduleInfo = new NativeArrayScheduleInfo<T>(array, batchStrategy);
            return this;
        }
        
        /// <summary>
        /// Specifies an instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used in the job in an
        /// Add context. 
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> that requires access.</param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
        public JobTaskWorkConfig RequireDataForAddAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VDWrapperForAddAsync wrapper = new VDWrapperForAddAsync(data);
            AddDataWrapper(wrapper);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DebugNotifyWorkDataOfUsage(wrapper.Type, DataUsage.Add);
#endif
            return this;
        }
        
        /// <summary>
        /// Specifies an instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used in the job in an
        /// Add context as well as a <see cref="VirtualData{TKey,TInstance}"/> that will be used as a results
        /// destination.
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> that requires access.</param>
        /// <param name="resultsDestination">
        /// The <see cref="VirtualData{TKey,TInstance}"/> to use as a results destination.
        /// NOTE: No access is necessary for this as it is just being used to point to where to write later on.
        /// </param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <typeparam name="TResult">The type of the result data</typeparam>
        /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
        public JobTaskWorkConfig RequireDataForAddAsync<TKey, TInstance, TResult>(VirtualData<TKey, TInstance> data, VirtualData<TKey, TResult> resultsDestination)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
            where TResult : unmanaged, IKeyedData<TKey>
        {
            RequireDataForAddAsync(data);
            RequireDataAsResultsDestination(resultsDestination);
            return this;
        }
        
        /// <summary>
        /// Specifies and instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used in the job in an
        /// Iterate context. 
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> that requires access.</param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
        public JobTaskWorkConfig RequireDataForIterateAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VDWrapperForIterateAsync wrapper = new VDWrapperForIterateAsync(data);
            AddDataWrapper(wrapper);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DebugNotifyWorkDataOfUsage(wrapper.Type, DataUsage.Iterate);
#endif
            return this;
        }
        
        /// <summary>
        /// Specifies and instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used in the job in an
        /// Update context. 
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> that requires access.</param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
        public JobTaskWorkConfig RequireDataForUpdateAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VDWrapperForUpdateAsync wrapper = new VDWrapperForUpdateAsync(data);
            AddDataWrapper(wrapper);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DebugNotifyWorkDataOfUsage(wrapper.Type, DataUsage.Update);
#endif
            return this;
        }
        
        /// <summary>
        /// Specifies and instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used in the job in an
        /// Results Destination context. 
        /// </summary>
        /// <param name="resultData">
        /// The <see cref="VirtualData{TKey,TInstance}"/> to use as a results destination.
        /// NOTE: No access is necessary for this as it is just being used to point to where to write later on.
        /// </param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TResult">The type of the result data</typeparam>
        /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
        public JobTaskWorkConfig RequireDataAsResultsDestination<TKey, TResult>(VirtualData<TKey, TResult> resultData)
            where TKey : unmanaged, IEquatable<TKey>
            where TResult : unmanaged, IKeyedData<TKey>
        {
            VDWrapperAsResultsDestination wrapper = new VDWrapperAsResultsDestination(resultData);
            AddDataWrapper(wrapper);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DebugNotifyWorkDataOfUsage(wrapper.Type, DataUsage.ResultsDestination);
#endif
            return this;
        }

        private JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_ScheduleInfo == null)
            {
                throw new InvalidOperationException($"No {nameof(IScheduleInfo)} was present. Please ensure that {nameof(ScheduleOn)} was called when configuring.");
            }
#endif

            int len = DataWrappers.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dataDependencies = new NativeArray<JobHandle>(len + 1, Allocator.Temp);

            for (int i = 0; i < DataWrappers.Count; ++i)
            {
                IDataWrapper wrapper = DataWrappers[i];
                dataDependencies[i] = wrapper.Acquire();
            }

            dataDependencies[len] = dependsOn;

            JobHandle delegateDependency = m_JobDataDelegate(JobHandle.CombineDependencies(dataDependencies), m_JobTaskWorkData, m_ScheduleInfo);

            foreach (IDataWrapper data in DataWrappers)
            {
                data.Release(delegateDependency);
            }

            return delegateDependency;
        }
        
        
        //*************************************************************************************************************
        // BULK SCHEDULERS
        //*************************************************************************************************************
        
        internal class JobTaskWorkConfigBulkScheduler : AbstractBulkScheduler<JobTaskWorkConfig>
        {
            public JobTaskWorkConfigBulkScheduler(List<JobTaskWorkConfig> list) : base(list)
            {
            }

            protected override JobHandle ScheduleItem(JobTaskWorkConfig item, JobHandle dependsOn)
            {
                return item.PrepareAndSchedule(dependsOn);
            }
        }
    }
}
