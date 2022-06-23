using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobSchedulingConfig
    {
        public delegate JobHandle JobDataDelegate(JobHandle dependsOn, JobData jobData, IScheduleInfo scheduleInfo);

        private readonly List<IDataWrapper> m_DataWrappers;
        private readonly JobDataDelegate m_JobDataDelegate;

        private readonly JobData m_JobData;
        private IScheduleInfo m_ScheduleInfo;
        
        internal JobSchedulingConfig(JobDataDelegate jobDataDelegate, AbstractTaskDriverSystem abstractTaskDriverSystem)
        {
            m_JobDataDelegate = jobDataDelegate;
            m_JobData = new JobData(abstractTaskDriverSystem);
            m_DataWrappers = new List<IDataWrapper>();
        }

        public JobSchedulingConfig ScheduleOn<TKey, TInstance>(VirtualData<TKey, TInstance> data, BatchStrategy batchStrategy)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            m_ScheduleInfo = new VirtualDataScheduleInfo<TKey, TInstance>(data, batchStrategy);
            return this;
        }

        public JobSchedulingConfig ScheduleOn<T>(NativeArray<T> array, BatchStrategy batchStrategy)
            where T : struct
        {
            m_ScheduleInfo = new NativeArrayScheduleInfo<T>(array, batchStrategy);
            return this;
        }

        public JobSchedulingConfig RequireDataForAddAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForAddAsync wrapper = new VDWrapperForAddAsync(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }
        
        public JobSchedulingConfig RequireDataForAddAsync<TKey, TInstance, TResult>(VirtualData<TKey, TInstance> data, VirtualData<TKey, TResult> resultsDestination)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
            where TResult : struct, IKeyedData<TKey>
        {
            RequireDataForAddAsync(data);
            RequireDataAsResultsDestination(resultsDestination);
            return this;
        }
        
        public JobSchedulingConfig RequireDataForIterateAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForIterateAsync wrapper = new VDWrapperForIterateAsync(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }
        
        public JobSchedulingConfig RequireDataForUpdateAsync<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperForUpdateAsync wrapper = new VDWrapperForUpdateAsync(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }

        public JobSchedulingConfig RequireDataAsResultsDestination<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            VDWrapperAsResultsDestination wrapper = new VDWrapperAsResultsDestination(data);
            AddDataWrapper(typeof(VirtualData<TKey, TInstance>), wrapper);
            return this;
        }

        private void AddDataWrapper(Type type, IDataWrapper dataWrapper)
        {
            m_JobData.AddDataWrapper(type, dataWrapper);
            m_DataWrappers.Add(dataWrapper);
        }
        
        
        public JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            int len = m_DataWrappers.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dataDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);

            for (int i = 0; i < m_DataWrappers.Count; ++i)
            {
                IDataWrapper wrapper = m_DataWrappers[i];
                dataDependencies[i] = wrapper.Acquire();
            }

            JobHandle delegateDependency = m_JobDataDelegate(JobHandle.CombineDependencies(dataDependencies), m_JobData, m_ScheduleInfo);

            foreach (IDataWrapper data in m_DataWrappers)
            {
                data.Release(delegateDependency);
            }

            return delegateDependency;
        }
    }   
}
