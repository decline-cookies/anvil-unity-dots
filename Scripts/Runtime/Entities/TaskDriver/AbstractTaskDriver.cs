using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
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
        private readonly TaskSet m_TaskSet;
        private readonly AbstractTaskDriverSystem m_TaskDriverSystem;


        /// <summary>
        /// Reference to the associated <see cref="World"/>
        /// </summary>
        public World World { get; }

        internal AbstractTaskDriver Parent { get; private set; }
        internal List<AbstractTaskDriver> SubTaskDrivers { get; }


        AbstractTaskDriverSystem ITaskSetOwner.TaskDriverSystem { get => m_TaskDriverSystem; }
        TaskSet ITaskSetOwner.TaskSet { get => m_TaskSet; }
        uint ITaskSetOwner.ID { get => m_ID; }


        protected AbstractTaskDriver(World world)
        {
            World = world;
            SubTaskDrivers = new List<AbstractTaskDriver>();
            m_TaskSet = new TaskSet(this);

            Type taskDriverType = GetType();
            Type taskDriverSystemType = TASK_DRIVER_SYSTEM_TYPE.MakeGenericType(taskDriverType);

            m_TaskDriverSystem = (AbstractTaskDriverSystem)World.GetExistingSystem(taskDriverSystemType);
            if (m_TaskDriverSystem == null)
            {
                m_TaskDriverSystem = (AbstractTaskDriverSystem)Activator.CreateInstance(taskDriverSystemType, World);
                World.AddSystem(m_TaskDriverSystem);
                World.GetOrCreateSystem<SimulationSystemGroup>().AddSystemToUpdateList(m_TaskDriverSystem);
            }

            m_ID = m_TaskDriverSystem.RegisterTaskDriver(this);


            RegisterWithManagementSystem();


            // //TODO: We can do this in hardening
            // HasCancellableData = TaskData.CancellableDataStreams.Count > 0
            //                   || SubTaskDrivers.Any(subTaskDriver => subTaskDriver.HasCancellableData)
            //                   || GoverningTaskSystem.HasCancellableData;
            //
            // //TODO: We can do this in hardening
            // CancelFlow.BuildRequestData();
        }

        private void RegisterWithManagementSystem()
        {
            DataSourceSystem dataSourceSystem = World.GetExistingSystem<DataSourceSystem>();
            dataSourceSystem.RegisterTaskDriver(this);
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


        protected TTaskDriver AddSubTaskDriver<TTaskDriver>(TTaskDriver subTaskDriver)
            where TTaskDriver : AbstractTaskDriver
        {
            subTaskDriver.Parent = this;
            SubTaskDrivers.Add(subTaskDriver);
            return subTaskDriver;
        }

        protected ISystemDataStream<TInstance> CreateSystemDataStream<TInstance>(CancelBehaviour cancelBehaviour = CancelBehaviour.Default)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            ISystemDataStream<TInstance> dataStream = m_TaskDriverSystem.GetOrCreateDataStream<TInstance>(cancelBehaviour);
            return dataStream;
        }

        protected IDriverDataStream<TInstance> CreateDriverDataStream<TInstance>(CancelBehaviour cancelBehaviour = CancelBehaviour.Default)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            IDriverDataStream<TInstance> dataStream = m_TaskSet.CreateDataStream<TInstance>(cancelBehaviour);
            return dataStream;
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - SYSTEM LEVEL
        //*************************************************************************************************************

        // protected IJobConfig ConfigureSystemJobToCancel<TInstance>(ISystemDataStream<TInstance> dataStream,
        //                                                            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
        //                                                            BatchStrategy batchStrategy)
        //     where TInstance : unmanaged, IEntityProxyInstance
        // {
        //     return m_TaskDriverSystem.TaskSet.ConfigureJobToCancel(dataStream,
        //                                                            scheduleJobFunction,
        //                                                            batchStrategy);
        // }

        protected IJobConfig ConfigureSystemJobToUpdate<TInstance>(ISystemDataStream<TInstance> dataStream,
                                                                   JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                   BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return m_TaskDriverSystem.TaskSet.ConfigureJobToUpdate(dataStream,
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
            return m_TaskSet.ConfigureJobTriggeredBy((DataStream<TInstance>)dataStream,
                                                     scheduleJobFunction,
                                                     batchStrategy);
        }

        // public IJobConfig ConfigureDriverCancelJobFor<TInstance>(IDriverDataStream<TInstance> dataStream,
        //                                                          in JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
        //                                                          BatchStrategy batchStrategy)
        //     where TInstance : unmanaged, IEntityProxyInstance
        // {
        //     return m_TaskSet.ConfigureCancelJobFor((CancellableDataStream<TInstance>)dataStream,
        //                                            scheduleJobFunction,
        //                                            batchStrategy);
        // }


        public IJobConfig ConfigureDriverJobTriggeredBy(EntityQuery entityQuery,
                                                        JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                        BatchStrategy batchStrategy)
        {
            return m_TaskSet.ConfigureJobTriggeredBy(entityQuery,
                                                     scheduleJobFunction,
                                                     batchStrategy);
        }

        // public IJobConfig ConfigureDriverJobWhenCancelComplete(in JobConfigScheduleDelegates.ScheduleCancelCompleteJobDelegate scheduleJobFunction,
        //                                                        BatchStrategy batchStrategy)
        // {
        //     return m_TaskSet.ConfigureJobWhenCancelComplete(TaskSet.CancelCompleteDataStream,
        //                                                     scheduleJobFunction,
        //                                                     batchStrategy);
        // }


        //TODO: #73 - Implement other job types
    }
}
