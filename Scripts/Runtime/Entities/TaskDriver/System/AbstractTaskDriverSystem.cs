using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract partial class AbstractTaskDriverSystem : AbstractAnvilSystemBase, ITaskSetOwner
    {
        private static readonly NoOpJobConfig NO_OP_JOB_CONFIG = new NoOpJobConfig();
        private static readonly List<AbstractTaskDriver> EMPTY_SUB_TASK_DRIVERS = new List<AbstractTaskDriver>();

        private readonly List<AbstractTaskDriver> m_TaskDrivers;

        private BulkJobScheduler<AbstractJobConfig> m_BulkJobScheduler;
        private bool m_IsHardened;
        private bool m_IsUpdatePhaseHardened;
        private bool m_HasCancellableData;

        public AbstractTaskDriverSystem TaskDriverSystem
        {
            get => this;
        }

        //Note - This represents the World that was passed in by the TaskDriver during this system's construction.
        //Normally a system doesn't get a World until OnCreate is called and the System.World will return null.
        //We need a valid World in the constructor so we get one and assign it to this property instead.
        public new World World { get; }
        public TaskSet TaskSet { get; }
        public uint ID { get; }

        public List<AbstractTaskDriver> SubTaskDrivers
        {
            get => EMPTY_SUB_TASK_DRIVERS;
        }

        public bool HasCancellableData
        {
            get
            {
                Debug_EnsureHardened();
                return m_HasCancellableData;
            }
        }

        protected AbstractTaskDriverSystem(World world)
        {
            World = world;
            TaskDriverManagementSystem taskDriverManagementSystem = World.GetExistingSystem<TaskDriverManagementSystem>();


            m_TaskDrivers = new List<AbstractTaskDriver>();

            ID = taskDriverManagementSystem.GetNextID();

            TaskSet = new TaskSet(this);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            Debug_EnsureWorldsAreTheSame();
        }

        protected override void OnDestroy()
        {
            //We don't own the TaskDrivers registered here, so we won't dispose them
            m_TaskDrivers.Clear();

            m_BulkJobScheduler?.Dispose();
            TaskSet.Dispose();

            base.OnDestroy();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}|{ID}";
        }

        public void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            m_TaskDrivers.Add(taskDriver);
        }

        public ISystemDataStream<TInstance> GetOrCreateDataStream<TInstance>(
            AbstractTaskDriver taskDriver,
            CancelRequestBehaviour cancelRequestBehaviour = CancelRequestBehaviour.Delete)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            EntityProxyDataStream<TInstance> dataStream = TaskSet.GetOrCreateDataStream<TInstance>(cancelRequestBehaviour);
            //Create a proxy DataStream that references the same data owned by the system but gives it the TaskDriver context
            return new EntityProxyDataStream<TInstance>(taskDriver, dataStream);
        }

        //We only want to register Jobs to the System once. However we still want to preserve the API in the TaskDriver.
        //If we have two or more TaskDrivers, we are guaranteed to have configured our System Jobs already configured.
        private bool HaveSystemLevelJobsBeenConfigured()
        {
            return m_TaskDrivers.Count >= 2;
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - SYSTEM LEVEL
        //*************************************************************************************************************

        public IResolvableJobConfigRequirements ConfigureSystemJobToUpdate<TInstance>(
            ISystemDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            //If we've already configured our system level jobs, we don't want to create duplicates so we return
            //the NO-OP config so that the API is preserved but it does nothing.
            if (HaveSystemLevelJobsBeenConfigured())
            {
                return NO_OP_JOB_CONFIG;
            }

            return TaskSet.ConfigureJobToUpdate(
                dataStream,
                scheduleJobFunction,
                batchStrategy);
        }

        public IResolvableJobConfigRequirements ConfigureSystemJobToCancel<TInstance>(
            ISystemDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            //If we've already configured our system level jobs, we don't want to create duplicates so we return
            //the NO-OP config so that the API is preserved but it does nothing.
            if (HaveSystemLevelJobsBeenConfigured())
            {
                return NO_OP_JOB_CONFIG;
            }

            return TaskSet.ConfigureJobToCancel(
                dataStream,
                scheduleJobFunction,
                batchStrategy);
        }

        //*************************************************************************************************************
        // HARDENING
        //*************************************************************************************************************

        public void Harden()
        {
            //This will get called multiple times but we only want to actually harden once
            if (m_IsHardened)
            {
                return;
            }

            m_IsHardened = true;

            //Harden our TaskSet
            TaskSet.Harden();

            m_HasCancellableData = TaskSet.ExplicitCancellationCount > 0;
        }

        public void HardenUpdatePhase()
        {
            Debug_EnsureNotHardenUpdatePhase();
            m_IsUpdatePhaseHardened = true;

            //Create the Bulk Job Scheduler for any jobs to run during this System's Update phase
            List<AbstractJobConfig> jobConfigs = new List<AbstractJobConfig>();
            TaskSet.AddJobConfigsTo(jobConfigs);
            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.AddJobConfigsTo(jobConfigs);
            }

            m_BulkJobScheduler = new BulkJobScheduler<AbstractJobConfig>(jobConfigs.ToArray());
        }

        public void AddResolvableDataStreamsTo(Type type, List<AbstractDataStream> dataStreams)
        {
            TaskSet.AddResolvableDataStreamsTo(type, dataStreams);
            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.TaskSet.AddResolvableDataStreamsTo(type, dataStreams);
            }
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        protected override void OnUpdate()
        {
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            dependsOn = m_BulkJobScheduler.Schedule(
                dependsOn,
                AbstractJobConfig.PREPARE_AND_SCHEDULE_FUNCTION);

            return dependsOn;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardenUpdatePhase()
        {
            if (m_IsUpdatePhaseHardened)
            {
                throw new InvalidOperationException($"Trying to Harden the Update Phase for {this} but {nameof(HardenUpdatePhase)} has already been called!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureHardened()
        {
            if (!m_IsHardened)
            {
                throw new InvalidOperationException($"Expected {this} to be Hardened but it hasn't yet!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWorldsAreTheSame()
        {
            if (World != base.World)
            {
                throw new InvalidOperationException($"The passed in World {World} is not the same as the automatically assigned one {base.World} in {nameof(OnCreate)}!");
            }
        }
    }
}