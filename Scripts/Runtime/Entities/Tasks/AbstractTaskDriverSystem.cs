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
    public abstract partial class AbstractTaskDriverSystem<TKey> : AbstractAnvilSystemBase
        where TKey : unmanaged, IEquatable<TKey>
    {
        private readonly List<AbstractTaskDriver<TKey>> m_TaskDrivers;
        private readonly VirtualDataLookup<TKey> m_InstanceDataLookup;
        private readonly CancelVirtualData<TKey> m_CancelData;
        private readonly List<JobTaskWorkConfig<TKey>> m_UpdateJobData;
        private readonly List<JobTaskWorkConfig<TKey>> m_CancelJobData;
        
        protected AbstractTaskDriverSystem()
        {
            m_InstanceDataLookup = new VirtualDataLookup<TKey>();
            m_CancelData = new CancelVirtualData<TKey>();
            
            m_TaskDrivers = new List<AbstractTaskDriver<TKey>>();
            m_UpdateJobData = new List<JobTaskWorkConfig<TKey>>();
            m_CancelJobData = new List<JobTaskWorkConfig<TKey>>();
        }
        
        protected override void OnDestroy()
        {
            m_InstanceDataLookup.Dispose();
            m_CancelData.Dispose();
            
            m_UpdateJobData.Clear();
            m_CancelJobData.Clear();
            
            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        protected JobTaskWorkConfig<TKey> ConfigureUpdateJob(JobTaskWorkConfig<TKey>.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig<TKey> config = new JobTaskWorkConfig<TKey>(scheduleJobDelegate, this, false);
            m_UpdateJobData.Add(config);
            return config;
        }
        
        protected JobTaskWorkConfig<TKey> ConfigureCancelJob(JobTaskWorkConfig<TKey>.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig<TKey> config = new JobTaskWorkConfig<TKey>(scheduleJobDelegate, this, true);
            m_CancelJobData.Add(config);
            return config;
        }
        
        //TODO: #39 - Some way to remove the update Job

        protected VirtualData<TKey, TInstance> CreateData<TInstance>(params AbstractVirtualData<TKey>[] sources)
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = VirtualData<TKey, TInstance>.Create(sources);
            m_InstanceDataLookup.AddData(virtualData);
            return virtualData;
        }

        protected VirtualData<TKey, TInstance> GetData<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            return m_InstanceDataLookup.GetData<TInstance>();
        }

        internal void RegisterTaskDriver(AbstractTaskDriver<TKey> taskDriver)
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
            dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver<TKey>.POPULATE_SCHEDULE_DELEGATE);
            
            //Consolidate our cancel data
            dependsOn = m_CancelData.ConsolidateForFrame(dependsOn, null);
            
            //Consolidate our instance data to operate on it
            dependsOn = m_InstanceDataLookup.ConsolidateForFrame(dependsOn, m_CancelData);
            
            //Handle anything that was cancelled and allow for generic work to happen in the derived class
            dependsOn = JobHandle.CombineDependencies(m_CancelJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig<TKey>.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE),
                                                      m_UpdateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig<TKey>.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE));
            
            //Have drivers consolidate their result data
            dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver<TKey>.CONSOLIDATE_SCHEDULE_DELEGATE);
            
            //Have drivers handle anything that was cancelled and do their own generic work
            dependsOn = JobHandle.CombineDependencies(m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver<TKey>.CANCEL_SCHEDULE_DELEGATE),
                                                      m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver<TKey>.UPDATE_SCHEDULE_DELEGATE));

            //Ensure this system's dependency is written back
            return dependsOn;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureTaskDriverSystemRelationship(AbstractTaskDriver<TKey> taskDriver)
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
