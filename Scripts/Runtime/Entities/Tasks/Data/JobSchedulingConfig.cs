using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobSchedulingConfig
    {
        public delegate JobHandle JobDataDelegate(JobHandle dependsOn, JobData jobData);
        
        private readonly Dictionary<Type, ScheduledVirtualData> m_ReferencedData = new Dictionary<Type, ScheduledVirtualData>();
        private readonly JobDataDelegate m_JobDataDelegate;

        private readonly JobData m_JobData;
        
        
        internal JobSchedulingConfig(JobDataDelegate jobDataDelegate, AbstractTaskDriverSystem abstractTaskDriverSystem)
        {
            m_JobDataDelegate = jobDataDelegate;
            m_JobData = new JobData(abstractTaskDriverSystem);
        }

        public JobSchedulingConfig ScheduleOn<TKey, TInstance>(VirtualData<TKey, TInstance> data, BatchStrategy batchStrategy)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            m_JobData.ScheduleWrapper = new VirtualDataScheduleWrapper<TKey, TInstance>(data, batchStrategy);
            return this;
        }

        public JobSchedulingConfig ScheduleOn<T>(NativeArray<T> array, BatchStrategy batchStrategy)
            where T : struct
        {
            m_JobData.ScheduleWrapper = new NativeArrayScheduleWrapper<T>(array, batchStrategy);
            return this;
        }

        public JobSchedulingConfig RequireDataForAdd<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Store in abstract form
            return this;
        }
        
        public JobSchedulingConfig RequireDataForAdd<TKey, TInstance, TResult>(VirtualData<TKey, TInstance> data, VirtualData<TKey, TResult> resultsDestination)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
            where TResult : struct, ILookupData<TKey>
        {
            //TODO: Store in abstract form
            return this;
        }
        
        public JobSchedulingConfig RequireDataForIterate<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Store in abstract form
            return this;
        }
        
        public JobSchedulingConfig RequireDataForUpdate<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Store in abstract form
            return this;
        }

        public JobSchedulingConfig RequireDataAsResultsDestination<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Store in abstract form
            return this;
        }
        
        
        public JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            int len = m_ReferencedData.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dataDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);

            int index = 0;
            foreach (ScheduledVirtualData data in m_ReferencedData.Values)
            {
                dataDependencies[index] = data.Acquire();
                index++;
            }

            JobHandle delegateDependency = m_JobDataDelegate(JobHandle.CombineDependencies(dataDependencies), m_JobData);

            foreach (ScheduledVirtualData data in m_ReferencedData.Values)
            {
                data.Release(delegateDependency);
            }

            return delegateDependency;
        }
    }   
}
