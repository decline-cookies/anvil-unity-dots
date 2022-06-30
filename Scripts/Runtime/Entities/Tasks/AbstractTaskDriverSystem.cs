using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A type of System that runs <see cref="AbstractTaskDriver"/>s during its update phase.
    /// </summary>
    public abstract partial class AbstractTaskDriverSystem : AbstractAnvilSystemBase
    {
        private readonly List<AbstractTaskDriver> m_TaskDrivers;
        private readonly VirtualDataLookup m_InstanceDataLookup;
        private readonly List<JobTaskWorkConfig> m_UpdateJobData;

        private readonly AbstractTaskDriver.TaskDriverPopulateBulkScheduler m_PopulateBulkScheduler;
        private readonly AbstractTaskDriver.TaskDriverUpdateBulkScheduler m_UpdateBulkScheduler;
        private readonly AbstractTaskDriver.TaskDriverConsolidateBulkScheduler m_ConsolidateBulkScheduler;
        private readonly JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler m_JobTaskWorkConfigBulkScheduler;

        protected AbstractTaskDriverSystem()
        {
            m_InstanceDataLookup = new VirtualDataLookup();
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
            base.OnCreate();
            InitSystemAfterCreate();
            InitUpdateJobConfiguration();
        }
        
        /// <summary>
        /// Hook to allow for creating new <see cref="VirtualData{TKey,TInstance}"/>
        /// via <see cref="CreateData{TKey,TInstance}"/>
        /// </summary>
        protected abstract void InitData();
    
        /// <summary>
        /// Hook to allow for additional System setup.
        /// This occurs once the System has been created via <see cref="OnCreate"/>
        /// </summary>
        protected abstract void InitSystemAfterCreate();
        
        /// <summary>
        /// Hook to allow for configuring jobs to execute during the system's update phase via
        /// <see cref="ConfigureUpdateJob"/>
        /// </summary>
        protected abstract void InitUpdateJobConfiguration();
        
        protected override void OnDestroy()
        {
            m_InstanceDataLookup.Dispose();
            
            m_UpdateJobData.Clear();

            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.Dispose();
            }
            m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        protected JobTaskWorkConfig ConfigureUpdateJob(JobTaskWorkConfig.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, this);
            m_UpdateJobData.Add(config);
            return config;
        }
        
        //TODO: #39 - Some way to remove the update Job

        protected VirtualData<TKey, TInstance> CreateData<TKey, TInstance>(params AbstractVirtualData[] sources)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = VirtualData<TKey, TInstance>.Create(sources);
            m_InstanceDataLookup.AddData(virtualData);
            return virtualData;
        }

        protected VirtualData<TKey, TInstance> GetData<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            return m_InstanceDataLookup.GetData<TKey, TInstance>();
        }

        internal void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            //TODO: Add guards - https://github.com/decline-cookies/anvil-unity-dots/pull/40/files#r910315944
            m_TaskDrivers.Add(taskDriver);
        }
        
        protected sealed override void OnUpdate()
        {
            //Have drivers be given the chance to add to the Instance Data
            JobHandle driversPopulateHandle = m_PopulateBulkScheduler.BulkSchedule(Dependency);
            
            //Consolidate our instance data to operate on it
            JobHandle consolidateInstancesHandle = m_InstanceDataLookup.ConsolidateForFrame(driversPopulateHandle);
            
            //TODO: #38 - Allow for cancels to occur
            
            //Allow the generic work to happen in the derived class
            JobHandle updateInstancesHandle = m_JobTaskWorkConfigBulkScheduler.BulkSchedule(consolidateInstancesHandle);
            
            //Have drivers consolidate their result data
            JobHandle driversConsolidateHandle = m_ConsolidateBulkScheduler.BulkSchedule(updateInstancesHandle);
            
            //TODO: #38 - Allow for cancels on the drivers to occur
            
            //Have drivers to do their own generic work
            JobHandle driversUpdateHandle = m_UpdateBulkScheduler.BulkSchedule(driversConsolidateHandle);

            //Ensure this system's dependency is written back
            Dependency = driversUpdateHandle;
        }
    }
}
