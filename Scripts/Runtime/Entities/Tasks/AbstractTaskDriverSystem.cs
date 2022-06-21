using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriverSystem : AbstractAnvilSystemBase
    {
        private readonly HashSet<AbstractTaskDriver> m_TaskDrivers = new HashSet<AbstractTaskDriver>();
        private readonly VirtualDataLookup m_InstanceData = new VirtualDataLookup();
        private readonly List<JobData> m_UpdateJobData = new List<JobData>();

        protected AbstractTaskDriverSystem()
        {
            CreateInstanceData();
        }

        protected abstract void CreateInstanceData();

        protected sealed override void OnCreate()
        {
            CreateUpdateJobs();
        }
        
        protected abstract void CreateUpdateJobs();
        protected override void OnDestroy()
        {
            m_InstanceData.Dispose();
            
            m_UpdateJobData.Clear();

            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.Dispose();
            }

            base.OnDestroy();
        }

        protected JobData CreateUpdateJob(JobData.JobDataDelegate jobDataDelegate, BatchStrategy batchStrategy)
        {
            JobData jobData = new JobData(jobDataDelegate, batchStrategy, this);
            m_UpdateJobData.Add(jobData);
            return jobData;
        }

        protected VirtualData<TKey, TInstance> CreateInstanceData<TKey, TInstance>(params IVirtualData[] sources)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = VirtualData<TKey, TInstance>.Create(sources);
            m_InstanceData.AddData(virtualData);
            return virtualData;
        }

        public VirtualData<TKey, TInstance> GetInstanceData<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            return m_InstanceData.GetData<TKey, TInstance>();
        }

        internal void AddTaskDriver(AbstractTaskDriver taskDriver)
        {
            m_TaskDrivers.Add(taskDriver);
        }
        
        protected sealed override void OnUpdate()
        {
            //Have drivers be given the chance to add to the Instance Data
            JobHandle driversPopulateHandle = PopulateDrivers(Dependency);
            
            //Consolidate our instance data to operate on it
            JobHandle consolidateInstancesHandle = m_InstanceData.ConsolidateForFrame(driversPopulateHandle);
            
            //TODO: Allow for cancels to occur
            
            //Allow the generic work to happen in the derived class
            JobHandle updateInstancesHandle = UpdateInstances(consolidateInstancesHandle);
            
            //Have drivers consolidate their result data
            JobHandle driversConsolidateHandle = ConsolidateDrivers(updateInstancesHandle);
            
            //TODO: Allow for cancels on the drivers to occur
            
            //Have drivers to do their own generic work
            JobHandle driversUpdateHandle = UpdateDrivers(driversConsolidateHandle);

            //Ensure this system's dependency is written back
            Dependency = driversUpdateHandle;
        }

        private JobHandle UpdateInstances(JobHandle dependsOn)
        {
            int len = m_UpdateJobData.Count;
            if (len == 0)
            {
                return dependsOn;
            }
            
            NativeArray<JobHandle> updateDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            for (int i = 0; i < len; ++i)
            {
                updateDependencies[i] = m_UpdateJobData[i].PrepareAndSchedule(dependsOn);
            }
            return JobHandle.CombineDependencies(updateDependencies);
        }
        

        //TODO: Can we merge code with Consolidate/Update/Cancel?
        private JobHandle PopulateDrivers(JobHandle dependsOn)
        {
            int len = m_TaskDrivers.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> taskDriverDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            int index = 0;
            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriverDependencies[index] = taskDriver.Populate(dependsOn);
                index++;
            }

            return JobHandle.CombineDependencies(taskDriverDependencies);
        }

        private JobHandle ConsolidateDrivers(JobHandle dependsOn)
        {
            int len = m_TaskDrivers.Count;
            if (len <= 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> taskDriverDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            int index = 0;
            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriverDependencies[index] = taskDriver.Consolidate(dependsOn);
                index++;
            }

            return JobHandle.CombineDependencies(taskDriverDependencies);
        }
        
        private JobHandle UpdateDrivers(JobHandle dependsOn)
        {
            int len = m_TaskDrivers.Count;
            if (len <= 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> taskDriverDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            int index = 0;
            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriverDependencies[index] = taskDriver.Update(dependsOn);
                index++;
            }

            return JobHandle.CombineDependencies(taskDriverDependencies);
        }
    }
}
