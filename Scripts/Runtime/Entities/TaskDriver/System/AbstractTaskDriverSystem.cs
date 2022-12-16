using Anvil.CSharp.Data;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract partial class AbstractTaskDriverSystem : AbstractAnvilSystemBase,
                                                               ITaskSetOwner
    {
        private static readonly NoOpJobConfig NO_OP_JOB_CONFIG = new NoOpJobConfig();
        
        private readonly IDProvider m_TaskDriverIDProvider;
        private readonly List<AbstractTaskDriver> m_TaskDrivers;

        private BulkJobScheduler<AbstractJobConfig> m_BulkJobScheduler;
        private bool m_IsHardened;


        public AbstractTaskDriverSystem TaskDriverSystem { get => this; }

        public new World World { get; }
        public TaskSet TaskSet { get; }
        public uint ID { get; }


        protected AbstractTaskDriverSystem(World world)
        {
            World = world;

            m_TaskDriverIDProvider = new IDProvider();
            m_TaskDrivers = new List<AbstractTaskDriver>();

            ID = m_TaskDriverIDProvider.GetNextID();

            TaskSet = new TaskSet(this);
        }

        protected override void OnDestroy()
        {
            m_TaskDriverIDProvider.Dispose();
            //We don't own the TaskDrivers registered here, so we won't dispose them
            m_TaskDrivers.Clear();

            m_BulkJobScheduler?.Dispose();
            TaskSet.Dispose();

            base.OnDestroy();
        }

        public override string ToString()
        {
            return GetType().GetReadableName();
        }

        public uint RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            uint taskDriverID = m_TaskDriverIDProvider.GetNextID();
            m_TaskDrivers.Add(taskDriver);
            return taskDriverID;
        }

        public ISystemDataStream<TInstance> GetOrCreateDataStream<TInstance>(CancelBehaviour cancelBehaviour = CancelBehaviour.Default)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSet.GetOrCreateDataStream<TInstance>(cancelBehaviour);
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - SYSTEM LEVEL
        //*************************************************************************************************************

        public IJobConfig ConfigureSystemJobToUpdate<TInstance>(ISystemDataStream<TInstance> dataStream,
                                                                JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            //We only want to register Jobs to the System once. However we still want to preserve the API in the TaskDriver.
            //If we have two or more TaskDrivers, we are guaranteed to have configured our System Jobs so we can just return 
            //a NO-OP job config that does nothing.
            if (m_TaskDrivers.Count >= 2)
            {
                return NO_OP_JOB_CONFIG;
            }

            return TaskSet.ConfigureJobToUpdate(dataStream,
                                                scheduleJobFunction,
                                                batchStrategy);
        }

        //*************************************************************************************************************
        // HARDENING
        //*************************************************************************************************************

        public void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            //Harden our TaskSet
            TaskSet.Harden();

            //Create the Bulk Job Scheduler for any jobs to run during this System's Update phase
            List<AbstractJobConfig> jobConfigs = new List<AbstractJobConfig>();
            TaskSet.AddJobConfigsTo(jobConfigs);
            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.AddJobConfigsTo(jobConfigs);
            }

            m_BulkJobScheduler = new BulkJobScheduler<AbstractJobConfig>(jobConfigs.ToArray());
        }

        protected override void OnUpdate()
        {
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            dependsOn = m_BulkJobScheduler.Schedule(dependsOn,
                                                    AbstractJobConfig.PREPARE_AND_SCHEDULE_FUNCTION);

            return dependsOn;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to Harden {this} but {nameof(Harden)} has already been called!");
            }
        }
    }
}
