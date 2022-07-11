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
        private const int SYSTEM_LEVEL_CANCEL_DATA_ID = -1;
        
        private readonly List<AbstractTaskDriver> m_TaskDrivers;
        private readonly VirtualDataLookup m_InstanceDataLookup;
        private readonly List<JobTaskWorkConfig> m_UpdateJobData;
        private readonly List<JobTaskWorkConfig> m_CancelJobData;

        private int m_TaskDriverID;

        internal CancelData CancelData
        {
            get;
        }

        protected AbstractTaskDriverSystem()
        {
            m_InstanceDataLookup = new VirtualDataLookup();
            CancelData = new CancelData(SYSTEM_LEVEL_CANCEL_DATA_ID);
            
            m_TaskDrivers = new List<AbstractTaskDriver>();
            m_UpdateJobData = new List<JobTaskWorkConfig>();
            m_CancelJobData = new List<JobTaskWorkConfig>();
        }
        
        protected override void OnDestroy()
        {
            m_InstanceDataLookup.Dispose();
            CancelData.Dispose();
            
            m_UpdateJobData.Clear();
            m_CancelJobData.Clear();
            
            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        internal int GetNextID()
        {
            //TODO: Make this robust
            int id = m_TaskDriverID;
            m_TaskDriverID++;
            return id;
        }

        protected JobTaskWorkConfig ConfigureUpdateJob(JobTaskWorkConfig.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, this, SYSTEM_LEVEL_CANCEL_DATA_ID);
            m_UpdateJobData.Add(config);
            return config;
        }
        
        protected JobTaskWorkConfig ConfigureCancelJob(JobTaskWorkConfig.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, this, SYSTEM_LEVEL_CANCEL_DATA_ID);
            m_CancelJobData.Add(config);
            return config;
        }
        
        //TODO: #39 - Some way to remove the update Job

        protected VirtualData<TInstance> CreateData<TInstance>(params AbstractVirtualData[] sources)
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = VirtualData<TInstance>.Create(sources);
            m_InstanceDataLookup.AddData(virtualData);
            return virtualData;
        }

        protected VirtualData<TInstance> GetData<TInstance>()
            where TInstance : unmanaged, IKeyedData
        {
            return m_InstanceDataLookup.GetData<TInstance>();
        }

        internal void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            Debug_EnsureTaskDriverSystemRelationship(taskDriver);
            m_TaskDrivers.Add(taskDriver);
        }
        
        protected override void OnUpdate()
        {
            Debug_EnsureNoDataLoss(m_InstanceDataLookup, m_UpdateJobData);
            
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            //Consolidate all the CancelData for all TaskDrivers and write to the System Data at the same time
            dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.CONSOLIDATE_CANCEL_SCHEDULE_DELEGATE);
            
            //Consolidate our system cancel data,
            //Have drivers be given the chance to add to the instance data
            //Propagate the TaskDriver cancel data to any subtask drivers they may have
            dependsOn = JobHandle.CombineDependencies(CancelData.ConsolidateForFrame(dependsOn),
                                                      m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.POPULATE_SCHEDULE_DELEGATE),
                                                      m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.PROPAGATE_CANCEL_TO_SUB_TASK_DRIVERS_SCHEDULE_DELEGATE));

            //Consolidate our instance data to operate on it
            dependsOn = m_InstanceDataLookup.ConsolidateForFrame(dependsOn, CancelData);
            
            //Handle anything that was cancelled and allow for generic work to happen in the derived class
            dependsOn = JobHandle.CombineDependencies(m_CancelJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE),
                                                      m_UpdateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE));
            
            //Have drivers consolidate their result data
            dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.CONSOLIDATE_SCHEDULE_DELEGATE);
            
            //Have drivers handle anything that was cancelled and do their own generic work
            dependsOn = JobHandle.CombineDependencies(m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.CANCEL_SCHEDULE_DELEGATE),
                                                      m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.UPDATE_SCHEDULE_DELEGATE));

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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDataLoss(VirtualDataLookup virtualDataLookup, List<JobTaskWorkConfig> updateJobs)
        {
            Dictionary<Type, AbstractVirtualData>.KeyCollection dataTypes = virtualDataLookup.DataTypes;
            HashSet<Type> jobTypes = new HashSet<Type>();

            foreach (JobTaskWorkConfig updateJob in updateJobs)
            {
                Dictionary<Type, AbstractVDWrapper>.KeyCollection updateJobDataTypes = updateJob.Debug_TaskWorkData.DataTypes;
                foreach (Type type in updateJobDataTypes)
                {
                    jobTypes.Add(type);
                }
            }
            
            //TODO: Should we ensure that we have actually scheduled a VDUpdater? Probably?
            foreach (Type dataType in dataTypes)
            {
                if (!jobTypes.Contains(dataType))
                {
                    throw new InvalidOperationException($"{this} has data registered of type {dataType} but there is no Update Job that operates on that type! This data will never be handled.");
                }
            }
        }
    }
}
