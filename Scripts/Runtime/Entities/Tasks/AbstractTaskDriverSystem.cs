using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract partial class AbstractTaskDriverSystem : AbstractAnvilSystemBase
    {
        private readonly List<AbstractTaskDriver> m_TaskDrivers;
        private readonly VirtualDataLookup m_InstanceData;
        private readonly List<JobTaskWorkConfig> m_UpdateJobData;

        private readonly AbstractTaskDriver.TaskDriverPopulateBulkScheduler m_PopulateBulkScheduler;
        private readonly AbstractTaskDriver.TaskDriverUpdateBulkScheduler m_UpdateBulkScheduler;
        private readonly AbstractTaskDriver.TaskDriverConsolidateBulkScheduler m_ConsolidateBulkScheduler;
        private readonly JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler m_JobTaskWorkConfigBulkScheduler;

        protected AbstractTaskDriverSystem()
        {
            m_InstanceData = new VirtualDataLookup();
            m_TaskDrivers = new List<AbstractTaskDriver>();
            m_UpdateJobData = new List<JobTaskWorkConfig>();

            m_PopulateBulkScheduler = new AbstractTaskDriver.TaskDriverPopulateBulkScheduler(m_TaskDrivers);
            m_UpdateBulkScheduler = new AbstractTaskDriver.TaskDriverUpdateBulkScheduler(m_TaskDrivers);
            m_ConsolidateBulkScheduler = new AbstractTaskDriver.TaskDriverConsolidateBulkScheduler(m_TaskDrivers);
            m_JobTaskWorkConfigBulkScheduler = new JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler(m_UpdateJobData);
            
            ConstructData();
        }

        private void ConstructData()
        {
            InitData();
        }
        protected sealed override void OnCreate()
        {
            InitSystemAfterCreate();
            InitUpdateJobs();
        }
        
        protected abstract void InitData();

        protected abstract void InitSystemAfterCreate();
        protected abstract void InitUpdateJobs();
        
        protected override void OnDestroy()
        {
            m_InstanceData.Dispose();
            
            m_UpdateJobData.Clear();

            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.Dispose();
            }
            m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        protected JobTaskWorkConfig CreateUpdateJob(JobTaskWorkConfig.JobDataDelegate jobDataDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(jobDataDelegate, this);
            m_UpdateJobData.Add(config);
            return config;
        }

        protected VirtualData<TKey, TInstance> CreateData<TKey, TInstance>(params AbstractVirtualData[] sources)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = VirtualData<TKey, TInstance>.Create(sources);
            m_InstanceData.AddData(virtualData);
            return virtualData;
        }

        protected VirtualData<TKey, TInstance> GetData<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
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
            JobHandle driversPopulateHandle = m_PopulateBulkScheduler.BulkSchedule(Dependency);
            
            //Consolidate our instance data to operate on it
            JobHandle consolidateInstancesHandle = m_InstanceData.ConsolidateForFrame(driversPopulateHandle);
            
            //TODO: Allow for cancels to occur
            
            //Allow the generic work to happen in the derived class
            JobHandle updateInstancesHandle = m_JobTaskWorkConfigBulkScheduler.BulkSchedule(consolidateInstancesHandle);
            
            //Have drivers consolidate their result data
            JobHandle driversConsolidateHandle = m_ConsolidateBulkScheduler.BulkSchedule(updateInstancesHandle);
            
            //TODO: Allow for cancels on the drivers to occur
            
            //Have drivers to do their own generic work
            JobHandle driversUpdateHandle = m_UpdateBulkScheduler.BulkSchedule(driversConsolidateHandle);

            //Ensure this system's dependency is written back
            Dependency = driversUpdateHandle;
        }
    }
}
