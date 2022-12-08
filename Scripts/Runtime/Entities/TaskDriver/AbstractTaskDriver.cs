using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a context specific Task done via Jobs over a wide array of multiple instances of data.
    /// The goal of a TaskDriver is to convert specific data into general data that the corresponding
    /// <see cref="AbstractTaskDriverSystem"/> will process en mass and in parallel. The results of that general processing
    /// are then picked up by the TaskDriver to be converted to specific data again and passed on to a sub task driver
    /// or to another general system. 
    /// </summary>
    public abstract class AbstractTaskDriver : AbstractAnvilBase,
                                               ITaskSetOwner
    {
        private static readonly Type TASK_DRIVER_SYSTEM_TYPE = typeof(TaskDriverSystem<>);


        private readonly uint m_ID;
        private readonly AbstractTaskDriver m_Parent;
        private readonly TaskSet m_TaskSet;


        /// <summary>
        /// Reference to the associated <see cref="World"/>
        /// </summary>
        public World World { get; }

        internal List<AbstractTaskDriver> SubTaskDrivers { get; }


        internal AbstractTaskDriverSystem TaskDriverSystem { get; }

        TaskSet ITaskSetOwner.TaskSet { get => m_TaskSet; }
        uint ITaskSetOwner.ID { get => m_ID; }


        protected AbstractTaskDriver(World world) : this(world, null)
        {
        }

        //This constructor can also be called via Reflection
        private AbstractTaskDriver(World world, AbstractTaskDriver parent)
        {
            World = world;
            m_Parent = parent;

            //Get or create the governing system for this Task Driver Type and register ourselves
            Type taskDriverType = GetType();
            Type taskDriverSystemType = TASK_DRIVER_SYSTEM_TYPE.MakeGenericType(taskDriverType);
            TaskDriverSystem = (AbstractTaskDriverSystem)World.GetOrCreateSystem(taskDriverSystemType);
            TaskDriverSystem.Init(World);
            m_ID = TaskDriverSystem.RegisterTaskDriver(this);


            m_TaskSet = TaskSetConstructionUtil.CreateTaskSetForTaskDriver(this);

            SubTaskDrivers = TaskDriverConstructionUtil.CreateSubTaskDrivers(this);

            RegisterWithManagementSystem();


            //TODO: We can do this in hardening
            HasCancellableData = TaskData.CancellableDataStreams.Count > 0
                              || SubTaskDrivers.Any(subTaskDriver => subTaskDriver.HasCancellableData)
                              || GoverningTaskSystem.HasCancellableData;

            //TODO: We can do this in hardening
            CancelFlow.BuildRequestData();
        }

        private void RegisterWithManagementSystem()
        {
            if (m_Parent != null)
            {
                return;
            }

            DataSourceSystem dataSourceSystem = World.GetExistingSystem<DataSourceSystem>();
            dataSourceSystem.RegisterTopLevelTaskDriver(this);
        }

        protected override void DisposeSelf()
        {
            //We own our sub task drivers so dispose them
            SubTaskDrivers.DisposeAllAndTryClear();

            m_TaskSet.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}|{m_ID}";
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - SYSTEM LEVEL
        //*************************************************************************************************************

        protected IJobConfig ConfigureSystemJobToCancel<TInstance>(ISystemDataStream<TInstance> dataStream,
                                                                JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                                BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskDriverSystem.TaskSet.ConfigureJobToCancel(dataStream,
                                                                 scheduleJobFunction,
                                                                 batchStrategy);
        }

        protected IJobConfig ConfigureSystemJobToUpdate<TInstance>(ISystemDataStream<TInstance> dataStream,
                                                                   JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                   BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskDriverSystem.TaskSet.ConfigureJobToUpdate(dataStream,
                                                                 scheduleJobFunction,
                                                                 batchStrategy);
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - DRIVER LEVEL
        //*************************************************************************************************************
        
        public IJobConfig ConfigureDriverJobTriggeredBy<TInstance>(IDriverDataStream<TInstance> dataStream,
                                                                               in JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                               BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSet.ConfigureJobTriggeredBy((DataStream<TInstance>)dataStream,
                                                   scheduleJobFunction,
                                                   batchStrategy);
        }

        public IJobConfig ConfigureDriverCancelJobFor<TInstance>(IDriverDataStream<TInstance> dataStream,
                                                           in JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                           BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSet.ConfigureCancelJobFor((CancellableDataStream<TInstance>)dataStream,
                                                 scheduleJobFunction,
                                                 batchStrategy);
        }


        public IJobConfig ConfigureDriverJobTriggeredBy(EntityQuery entityQuery,
                                                  JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                  BatchStrategy batchStrategy)
        {
            return TaskSet.ConfigureJobTriggeredBy(entityQuery,
                                                   scheduleJobFunction,
                                                   batchStrategy);
        }

        public IJobConfig ConfigureDriverJobWhenCancelComplete(in JobConfigScheduleDelegates.ScheduleCancelCompleteJobDelegate scheduleJobFunction,
                                                         BatchStrategy batchStrategy)
        {
            return TaskSet.ConfigureJobWhenCancelComplete(TaskSet.CancelCompleteDataStream,
                                                          scheduleJobFunction,
                                                          batchStrategy);
        }


        //TODO: #73 - Implement other job types
    }
}
