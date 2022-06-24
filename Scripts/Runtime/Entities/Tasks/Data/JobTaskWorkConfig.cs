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
        public class JobTaskWorkConfigBulkScheduler : AbstractBulkScheduler<JobTaskWorkConfig>
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
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            m_ScheduleInfo = new VirtualDataScheduleInfo<TKey, TInstance>(data, batchStrategy);
            return this;
        }

        public JobTaskWorkConfig ScheduleOn<T>(NativeArray<T> array, BatchStrategy batchStrategy)
            where T : struct
        {
            m_ScheduleInfo = new NativeArrayScheduleInfo<T>(array, batchStrategy);
            return this;
        }

        public JobTaskWorkConfig RequireDataForAddAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForAddAsync wrapper = new VDWrapperForAddAsync(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }
        
        public JobTaskWorkConfig RequireDataForAddAsync<TKey, TInstance, TResult>(VirtualData<TKey, TInstance> data, VirtualData<TKey, TResult> resultsDestination)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
            where TResult : struct, IKeyedData<TKey>
        {
            RequireDataForAddAsync(data);
            RequireDataAsResultsDestination(resultsDestination);
            return this;
        }
        
        public JobTaskWorkConfig RequireDataForIterateAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForIterateAsync wrapper = new VDWrapperForIterateAsync(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }
        
        public JobTaskWorkConfig RequireDataForUpdateAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForUpdateAsync wrapper = new VDWrapperForUpdateAsync(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }

        public JobTaskWorkConfig RequireDataAsResultsDestination<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperAsResultsDestination wrapper = new VDWrapperAsResultsDestination(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }
        
        public JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            int len = DataWrappers.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dataDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);

            for (int i = 0; i < DataWrappers.Count; ++i)
            {
                IDataWrapper wrapper = DataWrappers[i];
                dataDependencies[i] = wrapper.Acquire();
            }

            JobHandle delegateDependency = m_JobDataDelegate(JobHandle.CombineDependencies(dataDependencies), m_JobTaskWorkData, m_ScheduleInfo);

            foreach (IDataWrapper data in DataWrappers)
            {
                data.Release(delegateDependency);
            }

            return delegateDependency;
        }
    }   
}
