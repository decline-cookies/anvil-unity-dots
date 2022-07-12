using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A <see cref="AbstractTaskWorkConfig"/> specific for Jobs
    /// </summary>
    public class JobTaskWorkConfig : AbstractTaskWorkConfig
    {
        internal static readonly BulkScheduleDelegate<JobTaskWorkConfig> PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<JobTaskWorkConfig>, JobTaskWorkConfig>(nameof(PrepareAndSchedule), BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// The scheduling callback that is called when the job struct needs to be created and run through the job scheduler.
        /// </summary>
        public delegate JobHandle ScheduleJobDelegate(JobHandle dependsOn, TaskWorkData jobTaskWorkData, IScheduleInfo scheduleInfo);
        
        private readonly ScheduleJobDelegate m_ScheduleJobDelegate;
        private IScheduleInfo m_ScheduleInfo;

        internal JobTaskWorkConfig(ScheduleJobDelegate scheduleJobDelegate, AbstractTaskDriverSystem abstractTaskDriverSystem, int context) : base(abstractTaskDriverSystem, context)
        {
            m_ScheduleJobDelegate = scheduleJobDelegate;
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
        public JobTaskWorkConfig ScheduleOn<TInstance>(VirtualData<TInstance> data, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IKeyedData
        {
            Debug_EnsureNoDuplicateScheduleInfo();
            m_ScheduleInfo = new VirtualDataScheduleInfo<TInstance>(data, batchStrategy, false);
            return this;
        }
        
        //TODO: DISCUSS - CancelTaskWorkConfig vs JobTaskWorkConfig and abstract
        public JobTaskWorkConfig ScheduleOnForCancel<TInstance>(VirtualData<TInstance> data, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IKeyedData
        {
            Debug_EnsureNoDuplicateScheduleInfo();
            m_ScheduleInfo = new VirtualDataScheduleInfo<TInstance>(data, batchStrategy, true);
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
            Debug_EnsureNoDuplicateScheduleInfo();
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
        public JobTaskWorkConfig RequireDataForAddAsync<TInstance>(VirtualData<TInstance> data)
            where TInstance : unmanaged, IKeyedData
        {
            InternalRequireDataForAdd(data, true);
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
        public JobTaskWorkConfig RequireDataForAddAsync<TInstance, TResult>(VirtualData<TInstance> data, VirtualData<TResult> resultsDestination)
            where TInstance : unmanaged, IKeyedData
            where TResult : unmanaged, IKeyedData
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
        public JobTaskWorkConfig RequireDataForIterateAsync<TInstance>(VirtualData<TInstance> data)
            where TInstance : unmanaged, IKeyedData
        {
            InternalRequireDataForIterate(data, true);
            return this;
        }
        
        public JobTaskWorkConfig RequireCancelledDataForIterateAsync<TInstance>(VirtualData<TInstance> data)
            where TInstance : unmanaged, IKeyedData
        {
            InternalRequireCancelledDataForIterate(data, true);
            return this;
        }
        
        //TODO: Need to ensure all synchronous calls exist

        /// <summary>
        /// Specifies and instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used in the job in an
        /// Update context. 
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> that requires access.</param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
        public JobTaskWorkConfig RequireDataForUpdateAsync<TInstance>(VirtualData<TInstance> data)
            where TInstance : unmanaged, IKeyedData
        {
            InternalRequireDataForUpdate(data, true);
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
        public JobTaskWorkConfig RequireDataAsResultsDestination<TResult>(VirtualData<TResult> resultData)
            where TResult : unmanaged, IKeyedData
        {
            InternalRequireDataAsResultsDestination(resultData, true);
            return this;
        }

        public JobTaskWorkConfig RequireTaskDriverForCancellingAsync(AbstractTaskDriver taskDriver)
        {
            InternalRequireTaskDriverForCancelling(taskDriver, true);
            return this;
        }

        private JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            Debug_SetConfigurationStateComplete();
            Debug_EnsureScheduleInfoPresent();

            int len = DataWrappers.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dataDependencies = new NativeArray<JobHandle>(len + 1, Allocator.Temp);

            for (int i = 0; i < len; ++i)
            {
                AbstractVDWrapper wrapper = DataWrappers[i];
                dataDependencies[i] = wrapper.AcquireAsync();
            }

            dataDependencies[len] = dependsOn;

            JobHandle delegateDependency = m_ScheduleJobDelegate(JobHandle.CombineDependencies(dataDependencies), TaskWorkData, m_ScheduleInfo);

            foreach (AbstractVDWrapper data in DataWrappers)
            {
                data.ReleaseAsync(delegateDependency);
            }

            return delegateDependency;
        }

        //*************************************************************************************************************
        // SAFETY CHECKS
        //*************************************************************************************************************
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void Debug_EnsureNoDataLoss(object context, VirtualDataLookup virtualDataLookup, List<JobTaskWorkConfig> updateJobs, ref bool hasCheckedUpdateJobsForDataLoss)
        {
            //Only want to check this once since it won't change and we don't need to do this long check every frame
            if (hasCheckedUpdateJobsForDataLoss)
            {
                return;
            }

            hasCheckedUpdateJobsForDataLoss = true;
            
            Dictionary<Type, AbstractVirtualData> dataLookup = virtualDataLookup.Debug_DataLookup;
            HashSet<Type> updaterJobTypes = new HashSet<Type>();

            foreach (JobTaskWorkConfig updateJob in updateJobs)
            {
                Dictionary<Type, AbstractVDWrapper> wrappedDataLookup = updateJob.Debug_TaskWorkData.Debug_WrappedDataLookup;
                foreach (KeyValuePair<Type, AbstractVDWrapper> entry in wrappedDataLookup)
                {
                    if (entry.Value is VDWrapperForUpdate)
                    {
                        updaterJobTypes.Add(entry.Key);
                    }
                }
            }

            foreach (KeyValuePair<Type, AbstractVirtualData> entry in dataLookup)
            {
                if (!updaterJobTypes.Contains(entry.Key) && entry.Value.Intent != VirtualDataIntent.OneShot)
                {
                    throw new InvalidOperationException($"{context} has data registered of type {entry.Key} but there is no Update Job that uses a VDUpdater that operates on that type! This data will never be handled.");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateScheduleInfo()
        {
            if (m_ScheduleInfo != null)
            {
                throw new InvalidOperationException($"{nameof(ScheduleOn)} has already been called. This should only be called once");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureScheduleInfoPresent()
        {
            if (m_ScheduleInfo == null)
            {
                throw new InvalidOperationException($"No {nameof(IScheduleInfo)} was present. Please ensure that {nameof(ScheduleOn)} was called when configuring.");
            }
        }
    }
}
