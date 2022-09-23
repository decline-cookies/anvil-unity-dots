using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A type of System that runs <see cref="AbstractTaskDriver"/>s during its update phase.
    /// </summary>
    public abstract partial class AbstractTaskSystem<TTaskDriver, TTaskSystem> : AbstractAnvilSystemBase,
                                                                                 ITaskSystem
        where TTaskDriver : AbstractTaskDriver<TTaskDriver, TTaskSystem>
        where TTaskSystem : AbstractTaskSystem<TTaskDriver, TTaskSystem>
    {
        private readonly List<TTaskDriver> m_TaskDrivers;
        private readonly ByteIDProvider m_TaskDriverIDProvider;
        private readonly TaskFlowGraph m_TaskFlowGraph;

        private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_SystemJobConfigBulkJobSchedulerLookup;
        private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_DriverJobConfigBulkJobSchedulerLookup;

        private BulkJobScheduler<AbstractProxyDataStream> m_SystemDataStreamBulkJobScheduler;
        private BulkJobScheduler<AbstractProxyDataStream> m_DriverDataStreamBulkJobScheduler;

        private readonly RequestCancelDataStream m_RequestCancelDataStream;

        private bool m_IsHardened;

        public byte Context
        {
            get;
        }

        protected AbstractTaskSystem()
        {
            m_TaskDrivers = new List<TTaskDriver>();

            m_TaskDriverIDProvider = new ByteIDProvider();
            Context = m_TaskDriverIDProvider.GetNextID();

            m_RequestCancelDataStream = new RequestCancelDataStream();

            //TODO: Talk to Mike about this. The World property is null for the default world because systems are created via Activator.CreateInstance.
            //TODO: They don't go through the GetOrCreateSystem path. Is this the case for other worlds? Can we assume a null World is the default one?
            World currentWorld = World ?? World.DefaultGameObjectInjectionWorld;
            m_TaskFlowGraph = currentWorld.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            m_TaskFlowGraph.CreateDataStreams(this);

            //TODO: 3. Custom Update Job Types
            //TODO: Create the custom Update Job so we can parse to the different result channels. Required Hardening JobConfig as part of the TaskFlowGraph hardening
        }

        protected override void OnDestroy()
        {
            //We only want to dispose the data streams that we own, so only the system ones
            m_TaskFlowGraph.DisposeFor(this);

            //Clean up all the native arrays
            m_SystemDataStreamBulkJobScheduler?.Dispose();
            m_DriverDataStreamBulkJobScheduler?.Dispose();
            DisposeJobConfigBulkJobSchedulerLookup(m_SystemJobConfigBulkJobSchedulerLookup);
            DisposeJobConfigBulkJobSchedulerLookup(m_DriverJobConfigBulkJobSchedulerLookup);

            m_TaskDriverIDProvider.Dispose();

            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        private void DisposeJobConfigBulkJobSchedulerLookup(Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> lookup)
        {
            if (lookup == null)
            {
                return;
            }

            foreach (BulkJobScheduler<AbstractJobConfig> scheduler in lookup.Values)
            {
                scheduler.Dispose();
            }

            lookup.Clear();
        }

        private void Harden()
        {
            if (m_IsHardened)
            {
                return;
            }

            m_IsHardened = true;

            m_SystemDataStreamBulkJobScheduler = m_TaskFlowGraph.CreateDataStreamBulkJobSchedulerFor(this);
            m_DriverDataStreamBulkJobScheduler = m_TaskFlowGraph.CreateDataStreamBulkJobSchedulerFor(this, m_TaskDrivers);

            m_SystemJobConfigBulkJobSchedulerLookup = m_TaskFlowGraph.CreateJobConfigBulkJobSchedulerLookupFor(this);
            m_DriverJobConfigBulkJobSchedulerLookup = m_TaskFlowGraph.CreateJobConfigBulkJobSchedulerLookupFor(this, m_TaskDrivers);
        }

        //TODO: #39 - Some way to remove the update Job

        //*************************************************************************************************************
        // CONFIGURATION
        //*************************************************************************************************************

        internal byte RegisterTaskDriver(TTaskDriver taskDriver)
        {
            Debug_EnsureNotHardened(taskDriver);
            Debug_EnsureTaskDriverSystemRelationship(taskDriver);
            m_TaskDrivers.Add(taskDriver);

            return m_TaskDriverIDProvider.GetNextID();
        }

        internal IScheduleJobConfig ConfigurePopulateJob(ITaskDriver taskDriver,
                                                         IJobConfig.ScheduleJobDelegate scheduleJobFunction)
        {
            return ConfigureJob(taskDriver,
                                scheduleJobFunction,
                                TaskFlowRoute.Populate);
        }
        
        protected IJobConfig ConfigureUpdateJob<TInstance>(IUpdateJobConfig.ScheduleJobDelegate<TInstance> scheduleJobFunction,
                                                           ITaskStream<TInstance> dataStream,
                                                           BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            Debug_EnsureNotHardened(TaskFlowRoute.Update, null);

            UpdateJobConfig<TInstance> updateJobConfig = new UpdateJobConfig<TInstance>(m_TaskFlowGraph,
                                                                                        this,
                                                                                        null,
                                                                                        scheduleJobFunction,
                                                                                        dataStream,
                                                                                        batchStrategy,
                                                                                        m_RequestCancelDataStream);
            //TODO: Register with the Task Flow Graph
            
            return updateJobConfig;
        }

        private AbstractJobConfig ConfigureJob(ITaskDriver taskDriver,
                                       IJobConfig.ScheduleJobDelegate scheduleJobFunction,
                                       TaskFlowRoute route)
        {
            Debug_EnsureNotHardened(route, taskDriver);

            return m_TaskFlowGraph.CreateJobConfig(this,
                                                   taskDriver,
                                                   scheduleJobFunction,
                                                   route);
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        protected override void OnUpdate()
        {
            //TODO: Discuss with Mike about how we can get around this
            if (!m_TaskFlowGraph.IsHardened)
            {
                return;
            }

            Harden();
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            //Run all TaskDriver populate jobs to allow them to write to data streams (TaskDrivers -> generic TaskSystem data)
            dependsOn = ScheduleJobs(dependsOn,
                                     TaskFlowRoute.Populate,
                                     m_DriverJobConfigBulkJobSchedulerLookup);

            //Consolidate system data so that it can be operated on. (Was populated on previous step)
            dependsOn = m_SystemDataStreamBulkJobScheduler.Schedule(dependsOn,
                                                                    AbstractProxyDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            //TODO: #38 - Allow for cancels to occur

            //Schedule the Update Jobs to run on System Data
            dependsOn = ScheduleJobs(dependsOn,
                                     TaskFlowRoute.Update,
                                     m_SystemJobConfigBulkJobSchedulerLookup);

            // //Have drivers consolidate their data (Generic TaskSystem Update -> TaskDriver results)
            dependsOn = m_DriverDataStreamBulkJobScheduler.Schedule(dependsOn,
                                                                    AbstractProxyDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            //TODO: #38 - Allow for cancels on the drivers to occur

            // //Have drivers to do their own generic work if necessary
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
        private void Debug_EnsureTaskDriverSystemRelationship(TTaskDriver taskDriver)
        {
            if (taskDriver.TaskSystem != this)
            {
                throw new InvalidOperationException($"{taskDriver} is part of system {taskDriver.TaskSystem} but it should be {this}!");
            }

            if (m_TaskDrivers.Contains(taskDriver))
            {
                throw new InvalidOperationException($"Trying to add {taskDriver} to {this}'s list of Task Drivers but it is already there!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened(TaskFlowRoute route, ITaskDriver taskDriver)
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to create a {route} job on {TaskDebugUtil.GetLocation(this, taskDriver)} but the create phase for systems is complete! Please ensure that you configure your jobs in the {nameof(OnCreate)} or earlier.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened(TTaskDriver taskDriver)
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to register a {taskDriver} job but the create phase for systems is complete! Please ensure that all {typeof(TTaskDriver)}'s are created in {nameof(OnCreate)} or earlier.");
            }
        }
    }
}
