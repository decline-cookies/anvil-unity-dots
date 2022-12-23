using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

        private readonly TaskDriverManagementSystem m_TaskDriverManagementSystem;

        private bool m_IsHardened;
        private bool m_HasCancellableData;


        /// <summary>
        /// Reference to the associated <see cref="World"/>
        /// </summary>
        public World World { get; }

        internal AbstractTaskDriver Parent { get; private set; }
        internal List<AbstractTaskDriver> SubTaskDrivers { get; }

        internal AbstractTaskDriverSystem TaskDriverSystem { get; }
        internal TaskSet TaskSet { get; }
        internal uint ID { get; }

        AbstractTaskDriverSystem ITaskSetOwner.TaskDriverSystem { get => TaskDriverSystem; }
        TaskSet ITaskSetOwner.TaskSet { get => TaskSet; }
        uint ITaskSetOwner.ID { get => ID; }

        List<AbstractTaskDriver> ITaskSetOwner.SubTaskDrivers
        {
            get => SubTaskDrivers;
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
            m_TaskDriverManagementSystem = World.GetOrCreateSystem<TaskDriverManagementSystem>();
            
            SubTaskDrivers = new List<AbstractTaskDriver>();
            TaskSet = new TaskSet(this);

            Type taskDriverType = GetType();
            Type taskDriverSystemType = TASK_DRIVER_SYSTEM_TYPE.MakeGenericType(taskDriverType);

            TaskDriverSystem = (AbstractTaskDriverSystem)World.GetExistingSystem(taskDriverSystemType);
            if (TaskDriverSystem == null)
            {
                TaskDriverSystem = (AbstractTaskDriverSystem)Activator.CreateInstance(taskDriverSystemType, World);
                World.AddSystem(TaskDriverSystem);
                World.GetOrCreateSystem<SimulationSystemGroup>().AddSystemToUpdateList(TaskDriverSystem);
            }
            TaskDriverSystem.RegisterTaskDriver(this);

            ID = m_TaskDriverManagementSystem.GetNextID();
            m_TaskDriverManagementSystem.RegisterTaskDriver(this);
        }

        protected override void DisposeSelf()
        {
            //We own our sub task drivers so dispose them
            SubTaskDrivers.DisposeAllAndTryClear();

            TaskSet.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}|{ID}";
        }

        //*************************************************************************************************************
        // CONFIGURATION
        //*************************************************************************************************************

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
            ISystemDataStream<TInstance> dataStream = TaskDriverSystem.GetOrCreateDataStream<TInstance>(this, cancelBehaviour);
            return dataStream;
        }

        protected IDriverDataStream<TInstance> CreateDriverDataStream<TInstance>(CancelBehaviour cancelBehaviour = CancelBehaviour.Default)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            IDriverDataStream<TInstance> dataStream = TaskSet.CreateDataStream<TInstance>(cancelBehaviour);
            return dataStream;
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - SYSTEM LEVEL
        //*************************************************************************************************************

        // protected IResolvableJobConfigRequirements ConfigureSystemJobToCancel<TInstance>(ISystemDataStream<TInstance> dataStream,
        //                                                            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
        //                                                            BatchStrategy batchStrategy)
        //     where TInstance : unmanaged, IEntityProxyInstance
        // {
        //     return m_TaskDriverSystem.ConfigureJobToCancel(dataStream,
        //                                                            scheduleJobFunction,
        //                                                            batchStrategy);
        // }

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

        public IJobConfig ConfigureDriverJobTriggeredBy<TInstance>(IDriverDataStream<TInstance> dataStream,
                                                                   in JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                   BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSet.ConfigureJobTriggeredBy((EntityProxyDataStream<TInstance>)dataStream,
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
            return TaskSet.ConfigureJobTriggeredBy(entityQuery,
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

        //*************************************************************************************************************
        // HARDENING
        //*************************************************************************************************************

        internal void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;
            

            //Drill down so that the lowest Task Driver gets hardened
            foreach (AbstractTaskDriver subTaskDriver in SubTaskDrivers)
            {
                subTaskDriver.Harden();
            }
            
            //Harden our TaskDriverSystem if it hasn't been already
            TaskDriverSystem.Harden();

            //Harden our own TaskSet
            TaskSet.Harden();

            m_HasCancellableData = TaskSet.ExplicitCancellationCount > 0
                                || TaskDriverSystem.HasCancellableData
                                || SubTaskDrivers.Any(subtaskDriver => subtaskDriver.m_HasCancellableData);
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
