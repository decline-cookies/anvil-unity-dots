using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobTaskWorkConfig : AbstractTaskWorkConfig
    {
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

        public JobTaskWorkConfig ScheduleOn<TKey, TInstance>(VirtualData<TKey, TInstance> data, BatchStrategy batchStrategy)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            m_ScheduleInfo = new VirtualDataScheduleInfo<TKey, TInstance>(data, batchStrategy);
            return this;
        }

        public JobTaskWorkConfig ScheduleOn<T>(NativeArray<T> array, BatchStrategy batchStrategy)
            where T : unmanaged
        {
            m_ScheduleInfo = new NativeArrayScheduleInfo<T>(array, batchStrategy);
            return this;
        }

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

        public JobTaskWorkConfig RequireDataForAddAsync<TKey, TInstance, TResult>(VirtualData<TKey, TInstance> data, VirtualData<TKey, TResult> resultsDestination)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
            where TResult : unmanaged, IKeyedData<TKey>
        {
            RequireDataForAddAsync(data);
            RequireDataAsResultsDestination(resultsDestination);
            return this;
        }

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

        public JobTaskWorkConfig RequireDataAsResultsDestination<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VDWrapperAsResultsDestination wrapper = new VDWrapperAsResultsDestination(data);
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
    }
}
