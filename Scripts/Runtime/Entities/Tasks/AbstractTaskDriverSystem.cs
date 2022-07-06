using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly List<JobTaskWorkConfig> m_CancelJobData;
        protected AbstractTaskDriverSystem()
        {
            m_InstanceDataLookup = new VirtualDataLookup();
            m_TaskDrivers = new List<AbstractTaskDriver>();
            m_UpdateJobData = new List<JobTaskWorkConfig>();
            m_CancelJobData = new List<JobTaskWorkConfig>();
        }
        
        protected override void OnDestroy()
        {
            m_InstanceDataLookup.Dispose();
            
            m_UpdateJobData.Clear();
            m_CancelJobData.Clear();
            
            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        protected JobTaskWorkConfig ConfigureUpdateJob(JobTaskWorkConfig.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, this, false);
            m_UpdateJobData.Add(config);
            return config;
        }
        
        protected JobTaskWorkConfig ConfigureCancelJob(JobTaskWorkConfig.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, this, true);
            m_CancelJobData.Add(config);
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
            Debug_EnsureTaskDriverSystemRelationship(taskDriver);
            m_TaskDrivers.Add(taskDriver);
        }
        
        protected override void OnUpdate()
        {
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            //Have drivers be given the chance to add to the Instance Data
            dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.POPULATE_SCHEDULE_DELEGATE);
            
            //Consolidate our instance data to operate on it
            dependsOn = m_InstanceDataLookup.ConsolidateForFrame(dependsOn);
            
            
            //TODO: Can we do both Cancel and Update jobs at the same time? 
            //Handle anything that was cancelled
            dependsOn = m_CancelJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
            
            //Allow the generic work to happen in the derived class
            dependsOn = m_UpdateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
            
            
            //Have drivers consolidate their result data
            dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.CONSOLIDATE_SCHEDULE_DELEGATE);
            
            //TODO: Can we do both Cancel and Update jobs at the same time?
            //Have drivers handle anything that was cancelled
            dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.CANCEL_SCHEDULE_DELEGATE);
            
            //Have drivers to do their own generic work
            dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.UPDATE_SCHEDULE_DELEGATE);

            //Ensure this system's dependency is written back
            return dependsOn;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureTaskDriverSystemRelationship(AbstractTaskDriver taskDriver)
        {
            if (taskDriver.System != this)
            {
                throw new InvalidOperationException($"{taskDriver} is part of system {taskDriver.System} but it should be {this}!");
            }

            if (m_TaskDrivers.Contains(taskDriver))
            {
                throw new InvalidOperationException($"Trying to add {taskDriver} to {this}'s list of Task Drivers but it is already there!");
            }
        }
    }
}
