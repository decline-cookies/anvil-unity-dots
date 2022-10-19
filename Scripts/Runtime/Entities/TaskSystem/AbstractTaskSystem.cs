using Anvil.CSharp.Collections;
using Anvil.CSharp.Data;
using Anvil.CSharp.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// A <see cref="SystemBase"/> that is used in the Task System for running generic jobs on generic data en mass
    /// in conjunction with context specific <see cref="AbstractTaskDriver"/>s that will populate the generic data
    /// and receive the results, should they exist.
    /// </summary>
    public abstract class AbstractTaskSystem : AbstractAnvilSystemBase
    {
        private readonly ByteIDProvider m_TaskDriverContextProvider;
        private readonly List<AbstractJobConfig> m_JobConfigs;

        private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_SystemJobConfigBulkJobSchedulerLookup;
        private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_DriverJobConfigBulkJobSchedulerLookup;

        private BulkJobScheduler<AbstractEntityProxyDataStream> m_SystemDataStreamBulkJobScheduler;
        private BulkJobScheduler<AbstractEntityProxyDataStream> m_DriverDataStreamBulkJobScheduler;

        private BulkJobScheduler<TaskDriverCancellationPropagator> m_TaskDriversCancellationBulkJobScheduler;

        private TaskFlowGraph m_TaskFlowGraph;
        private bool m_IsHardened;

        /// <summary>
        /// The context of this <see cref="AbstractTaskSystem"/>.
        /// This will always be the first ID given by a <see cref="ByteIDProvider"/> and is used to differentiate
        /// between instances running in the generic system job(s) versus more specific Task Drivers.
        /// </summary>
        public byte Context { get; }

        internal CancelRequestsDataStream CancelRequestsDataStream { get; }
        internal List<AbstractTaskStream> TaskStreams { get; }
        internal List<AbstractTaskDriver> TaskDrivers { get; }


        protected AbstractTaskSystem()
        {
            m_TaskDriverContextProvider = new ByteIDProvider();
            TaskStreams = new List<AbstractTaskStream>();
            TaskDrivers = new List<AbstractTaskDriver>();
            m_JobConfigs = new List<AbstractJobConfig>();

            Context = m_TaskDriverContextProvider.GetNextID();

            TaskStreamFactory.CreateTaskStreams(this, TaskStreams);
            CancelRequestsDataStream = new CancelRequestsDataStream();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            //Initialize the TaskFlowGraph based on our World
            InitTaskFlowGraph(World);
        }

        private void InitTaskFlowGraph(World world)
        {
            //We could get called multiple times 
            if (m_TaskFlowGraph != null)
            {
                return;
            }
            
            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            //TODO: Investigate if we can just have a Register method with overloads for each type: #66, #67, and/or #68 - https://github.com/decline-cookies/anvil-unity-dots/pull/87/files#r995025025
            m_TaskFlowGraph.RegisterTaskSystem(this);
        }

        protected override void OnDestroy()
        {
            //Clean up all the cached native arrays hidden in the schedulers
            m_SystemDataStreamBulkJobScheduler?.Dispose();
            m_DriverDataStreamBulkJobScheduler?.Dispose();
            m_TaskDriversCancellationBulkJobScheduler?.Dispose();
            m_SystemJobConfigBulkJobSchedulerLookup?.DisposeAllValuesAndClear();
            m_DriverJobConfigBulkJobSchedulerLookup?.DisposeAllValuesAndClear();

            m_TaskDriverContextProvider.Dispose();

            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            TaskDrivers.Clear();

            //Dispose all the data we own
            TaskStreams.DisposeAllAndTryClear();
            m_JobConfigs.DisposeAllAndTryClear();
            CancelRequestsDataStream.Dispose();

            base.OnDestroy();
        }

        public override string ToString()
        {
            return GetType().GetReadableName();
        }

        internal void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            foreach (AbstractTaskDriver taskDriver in TaskDrivers)
            {
                taskDriver.Harden();
            }

            foreach (AbstractJobConfig jobConfig in m_JobConfigs)
            {
                jobConfig.Harden();
            }

            m_SystemDataStreamBulkJobScheduler = m_TaskFlowGraph.CreateDataStreamBulkJobSchedulerFor(this);
            m_DriverDataStreamBulkJobScheduler = m_TaskFlowGraph.CreateDataStreamBulkJobSchedulerFor(this, TaskDrivers);

            m_SystemJobConfigBulkJobSchedulerLookup = m_TaskFlowGraph.CreateJobConfigBulkJobSchedulerLookupFor(this);
            m_DriverJobConfigBulkJobSchedulerLookup = m_TaskFlowGraph.CreateJobConfigBulkJobSchedulerLookupFor(this, TaskDrivers);

            m_TaskDriversCancellationBulkJobScheduler = m_TaskFlowGraph.CreateTaskDriversCancellationBulkJobSchedulerFor(TaskDrivers);
        }

        //*************************************************************************************************************
        // CONFIGURATION
        //*************************************************************************************************************
        internal void ConfigureSystemJobs()
        {
            ConfigureJobs();
        }

        protected abstract void ConfigureJobs();

        internal byte RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            Debug_EnsureNotHardened(taskDriver);
            Debug_EnsureTaskDriverSystemRelationship(taskDriver);
            TaskDrivers.Add(taskDriver);

            //Init the task flow graph based on our World since we may have been constructed
            //before this system hit it's OnCreate. If we're already initialized, this is a no-op.
            InitTaskFlowGraph(taskDriver.World);

            return m_TaskDriverContextProvider.GetNextID();
        }

        internal IJobConfigRequirements ConfigureJobTriggeredBy(AbstractTaskDriver taskDriver,
                                                                EntityQuery entityQuery,
                                                                JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                                BatchStrategy batchStrategy)
        {
            EntityQueryJobConfig jobConfig = JobConfigFactory.CreateEntityQueryJobConfig(m_TaskFlowGraph,
                                                                                         this,
                                                                                         taskDriver,
                                                                                         entityQuery,
                                                                                         scheduleJobFunction,
                                                                                         batchStrategy);
            RegisterJob(taskDriver, jobConfig, TaskFlowRoute.Populate);

            return jobConfig;
        }

        internal IJobConfigRequirements ConfigureJobTriggeredBy<TInstance>(AbstractTaskDriver taskDriver,
                                                                           TaskStream<TInstance> taskStream,
                                                                           JobConfigScheduleDelegates.ScheduleTaskStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                           BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            TaskStreamJobConfig<TInstance> jobConfig = JobConfigFactory.CreateTaskStreamJobConfig(m_TaskFlowGraph,
                                                                                                  this,
                                                                                                  taskDriver,
                                                                                                  taskStream,
                                                                                                  scheduleJobFunction,
                                                                                                  batchStrategy);
            RegisterJob(taskDriver, jobConfig, TaskFlowRoute.Populate);

            return jobConfig;
        }

        private void RegisterJob(AbstractTaskDriver taskDriver,
                                 AbstractJobConfig jobConfig,
                                 TaskFlowRoute route)
        {
            Debug_EnsureNotHardened(route, taskDriver);
            m_TaskFlowGraph.RegisterJobConfig(jobConfig, route);
            if (taskDriver != null)
            {
                taskDriver.AddToJobConfigs(jobConfig);
            }
            else
            {
                m_JobConfigs.Add(jobConfig);
            }
        }


        protected IResolvableJobConfigRequirements ConfigureJobToCancel<TInstance>(SystemTaskStream<TInstance> taskStream,
                                                                                   JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                                                   BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancelJobConfig<TInstance> jobConfig = JobConfigFactory.CreateCancelJobConfig(m_TaskFlowGraph,
                                                                                          this,
                                                                                          null,
                                                                                          taskStream,
                                                                                          scheduleJobFunction,
                                                                                          batchStrategy);

            RegisterJob(null, jobConfig, TaskFlowRoute.Cancel);

            return jobConfig;
        }

        protected IResolvableJobConfigRequirements ConfigureJobToUpdate<TInstance>(SystemTaskStream<TInstance> taskStream,
                                                                                   JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                                   BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            UpdateJobConfig<TInstance> jobConfig = JobConfigFactory.CreateUpdateJobConfig(m_TaskFlowGraph,
                                                                                          this,
                                                                                          null,
                                                                                          taskStream,
                                                                                          CancelRequestsDataStream,
                                                                                          scheduleJobFunction,
                                                                                          batchStrategy);
            RegisterJob(null, jobConfig, TaskFlowRoute.Update);

            return jobConfig;
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
            //Run all TaskDriver populate jobs to allow them to write to data streams (TaskDrivers -> generic TaskSystem data)
            dependsOn = ScheduleJobs(dependsOn,
                                     TaskFlowRoute.Populate,
                                     m_DriverJobConfigBulkJobSchedulerLookup);

            //All TaskDrivers consolidate their CancelRequestsDataStream which also writes into this system's CancelRequestsDataStream.
            //At the same time, it will propagate the cancel request to any sub-task drivers
            dependsOn = m_TaskDriversCancellationBulkJobScheduler.Schedule(dependsOn,
                                                                           TaskDriverCancellationPropagator.CONSOLIDATE_AND_PROPAGATE_SCHEDULE_FUNCTION);

            //Consolidate system data so that it can be operated on. (Was populated on previous step)
            //The system data and the system's cancel requests can be consolidated in parallel
            dependsOn = JobHandle.CombineDependencies(CancelRequestsDataStream.ConsolidateForFrame(dependsOn),
                                                      m_SystemDataStreamBulkJobScheduler.Schedule(dependsOn,
                                                                                                  AbstractEntityProxyDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION));

            //Schedule the Update Jobs to run on System Data, we are guaranteed to have up to date Cancel Requests
            dependsOn = ScheduleJobs(dependsOn,
                                     TaskFlowRoute.Update,
                                     m_SystemJobConfigBulkJobSchedulerLookup);

            //Schedule the Cancel Jobs to run on System Data, we are guaranteed to have Cancelled instances now if they were requested
            dependsOn = ScheduleJobs(dependsOn,
                                     TaskFlowRoute.Cancel,
                                     m_SystemJobConfigBulkJobSchedulerLookup);

            // Have drivers consolidate their data (Generic TaskSystem Update -> TaskDriver results)
            dependsOn = m_DriverDataStreamBulkJobScheduler.Schedule(dependsOn,
                                                                    AbstractEntityProxyDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            //TODO: #72 - Allow for other phases as needed, try to make as parallel as possible

            // Have drivers to do their own generic work if necessary
            dependsOn = ScheduleJobs(dependsOn,
                                     TaskFlowRoute.Update,
                                     m_DriverJobConfigBulkJobSchedulerLookup);

            //Ensure this system's dependency is written back
            return dependsOn;
        }

        private JobHandle ScheduleJobs(JobHandle dependsOn,
                                       TaskFlowRoute route,
                                       Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> lookup)
        {
            BulkJobScheduler<AbstractJobConfig> scheduler = lookup[route];
            return scheduler.Schedule(dependsOn,
                                      AbstractJobConfig.PREPARE_AND_SCHEDULE_FUNCTION);
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureTaskDriverSystemRelationship(AbstractTaskDriver taskDriver)
        {
            if (taskDriver.TaskSystem != this)
            {
                throw new InvalidOperationException($"{taskDriver} is part of system {taskDriver.TaskSystem} but it should be {this}!");
            }

            if (TaskDrivers.Contains(taskDriver))
            {
                throw new InvalidOperationException($"Trying to add {taskDriver} to {this}'s list of Task Drivers but it is already there!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened(TaskFlowRoute route, AbstractTaskDriver taskDriver)
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to create a {route} job on {TaskDebugUtil.GetLocationName(this, taskDriver)} but the create phase for systems is complete! Please ensure that you configure your jobs in the {nameof(ConfigureJobs)}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened(AbstractTaskDriver taskDriver)
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to register a {taskDriver} job but the create phase for systems is complete! Please ensure that all {taskDriver}'s are created in {nameof(OnCreate)} or earlier.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to Harden {this} but we already are!");
            }
        }
    }
}
