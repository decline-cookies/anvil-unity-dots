using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Given a "Task" to complete, the TaskDriver handles ensuring it is populated, processed and completed by
    /// defining the data needed, any subtasks to accomplish and the Unity Jobs to do the work required.
    /// TaskDrivers are contextual, meaning that the work they accomplish is unique to their usage in different parts
    /// of an application or as different sub task drivers as part of larger, more complex Task Drivers. 
    /// The goal of a TaskDriver is to convert the specific contextual data into general agnostic data that the corresponding
    /// <see cref="AbstractTaskDriverSystem"/> will process in parallel. The results of that system processing
    /// are then picked up by the TaskDriver to be converted to specific contextual data again and passed on to
    /// a sub task driver or to another system. 
    /// </summary>
    public abstract class AbstractTaskDriver : AbstractAnvilBase,
                                               ITaskSetOwner
    {
        private static readonly Type TASK_DRIVER_SYSTEM_TYPE = typeof(TaskDriverSystem<>);
        private static readonly Type COMPONENT_SYSTEM_GROUP_TYPE = typeof(ComponentSystemGroup);

        private readonly List<AbstractTaskDriver> m_SubTaskDrivers;
        private readonly uint m_ID;

        private bool m_IsHardened;
        private bool m_HasCancellableData;

        /// <summary>
        /// Reference to the associated <see cref="World"/>
        /// </summary>
        public World World { get; }

        internal AbstractTaskDriver Parent { get; private set; }
        internal AbstractTaskDriverSystem TaskDriverSystem { get; }
        internal TaskSet TaskSet { get; }

        AbstractTaskDriverSystem ITaskSetOwner.TaskDriverSystem { get => TaskDriverSystem; }
        TaskSet ITaskSetOwner.TaskSet { get => TaskSet; }
        uint ITaskSetOwner.ID { get => m_ID; }

        List<AbstractTaskDriver> ITaskSetOwner.SubTaskDrivers
        {
            get => m_SubTaskDrivers;
        }

        bool ITaskSetOwner.HasCancellableData
        {
            get
            {
                Debug_EnsureHardened();
                return m_HasCancellableData;
            }
        }

        protected AbstractTaskDriver(World world)
        {
            World = world;
            TaskDriverManagementSystem taskDriverManagementSystem = TaskDriverManagementSystem.GetOrCreateForWorld(world);

            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            TaskSet = new TaskSet(this);

            Type taskDriverType = GetType();
            Type taskDriverSystemType = TASK_DRIVER_SYSTEM_TYPE.MakeGenericType(taskDriverType);

            //If this is the first TaskDriver of this type, then the System will have been created for this World.
            TaskDriverSystem = (AbstractTaskDriverSystem)World.GetExistingSystem(taskDriverSystemType);
            //If not, then we will want to explicitly create it and ensure it is part of the lifecycle.
            if (TaskDriverSystem == null)
            {
                TaskDriverSystem = (AbstractTaskDriverSystem)Activator.CreateInstance(taskDriverSystemType, World);
                World.AddSystem(TaskDriverSystem);
                ComponentSystemGroup systemGroup = GetSystemGroup();
                systemGroup.AddSystemToUpdateList(TaskDriverSystem);
            }

            TaskDriverSystem.RegisterTaskDriver(this);

            m_ID = taskDriverManagementSystem.GetNextID();
            taskDriverManagementSystem.RegisterTaskDriver(this);
        }

        protected override void DisposeSelf()
        {
            //We own our sub task drivers so dispose them
            m_SubTaskDrivers.DisposeAllAndTryClear();

            TaskSet.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}|{m_ID}";
        }

        private ComponentSystemGroup GetSystemGroup()
        {
            Type systemGroupType = GetSystemGroupType();
            if (!COMPONENT_SYSTEM_GROUP_TYPE.IsAssignableFrom(systemGroupType))
            {
                throw new InvalidOperationException($"Tried to get the {COMPONENT_SYSTEM_GROUP_TYPE.GetReadableName()} for {this} but {systemGroupType.GetReadableName()} is not a valid group type!");
            }

            return (ComponentSystemGroup)World.GetOrCreateSystem(systemGroupType);
        }

        private Type GetSystemGroupType()
        {
            Type type = GetType();
            UpdateInGroupAttribute updateInGroupAttribute = type.GetCustomAttribute<UpdateInGroupAttribute>();
            return updateInGroupAttribute == null
                ? typeof(SimulationSystemGroup)
                : updateInGroupAttribute.GroupType;
        }

        //*************************************************************************************************************
        // CONFIGURATION
        //*************************************************************************************************************

        protected TTaskDriver AddSubTaskDriver<TTaskDriver>(TTaskDriver subTaskDriver)
            where TTaskDriver : AbstractTaskDriver
        {
            subTaskDriver.Parent = this;
            m_SubTaskDrivers.Add(subTaskDriver);
            return subTaskDriver;
        }

        protected ISystemDataStream<TInstance> CreateSystemDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour = CancelRequestBehaviour.Delete)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            ISystemDataStream<TInstance> dataStream = TaskDriverSystem.GetOrCreateDataStream<TInstance>(this, cancelRequestBehaviour);
            return dataStream;
        }

        protected IDriverDataStream<TInstance> CreateDriverDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour = CancelRequestBehaviour.Delete)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            IDriverDataStream<TInstance> dataStream = TaskSet.CreateDataStream<TInstance>(cancelRequestBehaviour);
            return dataStream;
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - SYSTEM LEVEL
        //*************************************************************************************************************

        protected IResolvableJobConfigRequirements ConfigureSystemJobToCancel<TInstance>(ISystemDataStream<TInstance> dataStream,
                                                                                         JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                                                         BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskDriverSystem.ConfigureSystemJobToCancel(dataStream,
                                                               scheduleJobFunction,
                                                               batchStrategy);
        }

        protected IResolvableJobConfigRequirements ConfigureSystemJobToUpdate<TInstance>(ISystemDataStream<TInstance> dataStream,
                                                                                         JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                                         BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskDriverSystem.ConfigureSystemJobToUpdate(dataStream,
                                                               scheduleJobFunction,
                                                               batchStrategy);
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - DRIVER LEVEL
        //*************************************************************************************************************

        /// <summary>
        /// Configures a Job that is triggered by instances being present in the passed in <see cref="IDriverDataStream{TInstance}"/>
        /// </summary>
        /// <param name="dataStream">The <see cref="IDriverDataStream{TInstance}"/> to trigger the job off of.</param>
        /// <param name="scheduleJobFunction">The scheduling function to call to schedule the job.</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to use for executing the job.</param>
        /// <typeparam name="TInstance">The type of instance contained in the <see cref="IDriverDataStream{TInstance}"/></typeparam>
        /// <returns>A <see cref="IJobConfig"/> to allow for chaining more configuration options.</returns>
        public IJobConfig ConfigureDriverJobTriggeredBy<TInstance>(IDriverDataStream<TInstance> dataStream,
                                                                   JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                   BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSet.ConfigureJobTriggeredBy((EntityProxyDataStream<TInstance>)dataStream,
                                                   scheduleJobFunction,
                                                   batchStrategy);
        }

        /// <summary>
        /// Configures a Job that is triggered by <see cref="Entity"/> or <see cref="IComponentData"/> being
        /// present in the passed in <see cref="EntityQuery"/>
        /// </summary>
        /// <param name="entityQuery">The <see cref="EntityQuery"/> to trigger the job off of.</param>
        /// <param name="scheduleJobFunction">The scheduling function to call to schedule the job.</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to use for executing the job.</param>
        /// <returns>A <see cref="IJobConfig"/> to allow for chaining more configuration options.</returns>
        public IJobConfig ConfigureDriverJobTriggeredBy(EntityQuery entityQuery,
                                                        JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                        BatchStrategy batchStrategy)
        {
            return TaskSet.ConfigureJobTriggeredBy(entityQuery,
                                                   scheduleJobFunction,
                                                   batchStrategy);
        }

        /// <summary>
        /// Configures a Job that is triggered by the cancellation of instances in this <see cref="AbstractTaskDriver"/>
        /// completing.
        /// </summary>
        /// <param name="scheduleJobFunction">The scheduling function to call to schedule the job.</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to use for executing the job.</param>
        /// <returns>A <see cref="IJobConfig"/> to allow for chaining more configuration options.</returns>
        public IJobConfig ConfigureDriverJobWhenCancelComplete(JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<CancelComplete> scheduleJobFunction,
                                                               BatchStrategy batchStrategy)
        {
            return TaskSet.ConfigureJobWhenCancelComplete(scheduleJobFunction,
                                                          batchStrategy);
        }


        //TODO: #73 - Implement other job types
        
        //*************************************************************************************************************
        // EXTERNAL USAGE
        //*************************************************************************************************************
        
        /// <summary>
        /// Gets a <see cref="DataStreamActiveReader{CancelComplete}"/> for use in a job outside the Task Driver context.
        /// Requires a call to <see cref="ReleaseCancelCompleteReaderAsync"/> after scheduling the job.
        /// </summary>
        /// <returns>The <see cref="DataStreamActiveReader{CancelComplete}"/></returns>
        public DataStreamActiveReader<CancelComplete> AcquireCancelCompleteReaderAsync()
        {
            return TaskSet.AcquireCancelCompleteReaderAsync();
        }
        
        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="DataStreamActiveReader{CancelComplete}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleaseCancelCompleteReaderAsync(JobHandle dependsOn)
        {
            TaskSet.ReleaseCancelCompleteReaderAsync(dependsOn);
        }
        
        /// <summary>
        /// Gets a <see cref="DataStreamActiveReader{CancelComplete}"/> for use on the main thread outside the Task Driver
        /// context.
        /// Requires a call to <see cref="ReleaseCancelCompleteReader"/> when done.
        /// </summary>
        /// <returns>The <see cref="DataStreamActiveReader{CancelComplete}"/></returns>
        public DataStreamActiveReader<CancelComplete> AcquireCancelCompleteReader()
        {
            return TaskSet.AcquireCancelCompleteReader();
        }
        
        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="DataStreamActiveReader{CancelComplete}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void ReleaseCancelCompleteReader()
        {
            TaskSet.ReleaseCancelCompleteReader();
        }
        
        /// <summary>
        /// Gets a <see cref="CancelRequestsWriter"/> for use in a job outside the Task Driver context.
        /// Requires a call to <see cref="ReleaseCancelRequestsWriterAsync"/> after scheduling the job.
        /// </summary>
        /// <returns>The <see cref="CancelRequestsWriter"/></returns>
        public CancelRequestsWriter AcquireCancelRequestsWriterAsync()
        {
            return TaskSet.AcquireCancelRequestsWriterAsync();
        }
        
        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="CancelRequestsWriter"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleaseCancelRequestsWriterAsync(JobHandle dependsOn)
        {
            TaskSet.ReleaseCancelRequestsWriterAsync(dependsOn);
        }
        
        /// <summary>
        /// Gets a <see cref="CancelRequestsWriter"/> for use on the main thread outside the Task Driver
        /// context.
        /// Requires a call to <see cref="ReleaseCancelRequestsWriter"/> when done.
        /// </summary>
        /// <returns>The <see cref="CancelRequestsWriter"/></returns>
        public CancelRequestsWriter AcquireCancelRequestsWriter()
        {
            return TaskSet.AcquireCancelRequestsWriter();
        }
        
        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="CancelRequestsWriter"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void ReleaseCancelRequestsWriter()
        {
            TaskSet.ReleaseCancelRequestsWriter();
        }

        //*************************************************************************************************************
        // HARDENING
        //*************************************************************************************************************

        internal void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;


            //Drill down so that the lowest Task Driver gets hardened
            foreach (AbstractTaskDriver subTaskDriver in m_SubTaskDrivers)
            {
                subTaskDriver.Harden();
            }

            //Harden our TaskDriverSystem if it hasn't been already
            TaskDriverSystem.Harden();

            //Harden our own TaskSet
            TaskSet.Harden();

            //TODO: #138 - Can we consolidate this into the TaskSet and have TaskSets aware of parenting instead
            m_HasCancellableData = TaskSet.ExplicitCancellationCount > 0
                                || TaskDriverSystem.HasCancellableData
                                || m_SubTaskDrivers.Any(subtaskDriver => subtaskDriver.m_HasCancellableData);
        }

        internal void AddJobConfigsTo(List<AbstractJobConfig> jobConfigs)
        {
            TaskSet.AddJobConfigsTo(jobConfigs);
        }

        void ITaskSetOwner.AddResolvableDataStreamsTo(Type type, List<AbstractDataStream> dataStreams)
        {
            TaskSet.AddResolvableDataStreamsTo(type, dataStreams);
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureHardened()
        {
            if (!m_IsHardened)
            {
                throw new InvalidOperationException($"Expected {this} to be Hardened but it hasn't yet!");
            }
        }
    }
}
