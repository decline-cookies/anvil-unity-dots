using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <inheritdoc cref="AbstractTaskDriver"/>
    /// <typeparam name="TTaskSystem">The type of <see cref="AbstractTaskSystem"/></typeparam>
    public abstract class AbstractTaskDriver<TTaskSystem> : AbstractTaskDriver
        where TTaskSystem : AbstractTaskSystem
    {
        /// <summary>
        /// Reference to the associated <typeparamref name="TTaskSystem"/>
        /// Hides the base reference to the abstract version.
        /// </summary>
        protected new TTaskSystem TaskSystem
        {
            get => (TTaskSystem)base.TaskSystem;
        }
        
        protected AbstractTaskDriver(World world) : base(world, typeof(TTaskSystem))
        {
        }
    }
    
    /// <summary>
    /// Represents a context specific Task done via Jobs over a wide array of multiple instances of data.
    /// The goal of a TaskDriver is to convert specific data into general data that the corresponding
    /// <see cref="AbstractTaskSystem"/> will process en mass and in parallel. The results of that general processing
    /// are then picked up by the TaskDriver to be converted to specific data again and passed on to a sub task driver
    /// or to another general system. 
    /// </summary>
    //TODO: #74 - Add support for Sub-Task Drivers properly when building an example nested TaskDriver.
    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
        private readonly List<AbstractTaskDriver> m_SubTaskDrivers;
        private readonly TaskFlowGraph m_TaskFlowGraph;
        private readonly List<AbstractJobConfig> m_JobConfigs;

        private bool m_IsHardened;
        
        /// <summary>
        /// The context associated with this TaskDriver. Will be unique to the corresponding
        /// <see cref="AbstractTaskSystem"/> and any other TaskDrivers of the same type.
        /// </summary>
        public byte Context { get; }
        
        /// <summary>
        /// Reference to the associated <see cref="AbstractTaskSystem"/>
        /// </summary>
        internal AbstractTaskSystem TaskSystem { get; }
        
        /// <summary>
        /// Reference to the associated <see cref="World"/>
        /// </summary>
        public World World { get; }

        internal CancelRequestsDataStream CancelRequestsDataStream { get; }
        internal List<AbstractEntityProxyDataStream> DataStreams { get; }
        internal TaskDriverCancellationPropagator CancellationPropagator { get; private set; }

        protected AbstractTaskDriver(World world, Type systemType)
        {
            //We can't just pull this off the System because we might have triggered it's creation via
            //world.GetOrCreateSystem and it's OnCreate hasn't occured yet so it's World is still null.
            World = world;
            
            TaskSystem = (AbstractTaskSystem)world.GetOrCreateSystem(systemType);
            Context = TaskSystem.RegisterTaskDriver(this);
            
            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            DataStreams = new List<AbstractEntityProxyDataStream>();
            m_JobConfigs = new List<AbstractJobConfig>();
            
            CancelRequestsDataStream = new CancelRequestsDataStream();
            DataStreamFactory.CreateDataStreams(this, DataStreams);
            TaskDriverFactory.CreateSubTaskDrivers(this, m_SubTaskDrivers);
            
            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            //TODO: Investigate if we need this here: #66, #67, and/or #68 - https://github.com/decline-cookies/anvil-unity-dots/pull/87/files#r995032614
            m_TaskFlowGraph.RegisterTaskDriver(this);
        }

        protected override void DisposeSelf()
        {
            //We own our sub task drivers so dispose them
            m_SubTaskDrivers.DisposeAllAndTryClear();
            //Dispose all the data we own
            DataStreams.DisposeAllAndTryClear();
            m_JobConfigs.DisposeAllAndTryClear();
            CancelRequestsDataStream.Dispose();
            CancellationPropagator?.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}|{Context}";
        }

        internal void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;
            
            foreach (AbstractJobConfig jobConfig in m_JobConfigs)
            {
                jobConfig.Harden();
            }

            CancellationPropagator = new TaskDriverCancellationPropagator(this);
        }

        internal void AddCancelRequestsTo(List<CancelRequestsDataStream> cancelRequests)
        {
            foreach (AbstractTaskDriver subTaskDriver in m_SubTaskDrivers)
            {
                cancelRequests.Add(subTaskDriver.CancelRequestsDataStream);
                cancelRequests.Add(subTaskDriver.TaskSystem.CancelRequestsDataStream);
                subTaskDriver.AddCancelRequestsTo(cancelRequests);
            }
        }

        //*************************************************************************************************************
        // CONFIGURATION
        //*************************************************************************************************************

        internal void AddToJobConfigs(AbstractJobConfig jobConfig)
        {
            m_JobConfigs.Add(jobConfig);
        }
        
        //TODO: Should Task Drivers should have no jobs
        public IJobConfigRequirements ConfigureJobTriggeredBy<TInstance>(EntityProxyDataStream<TInstance> taskStream,
                                                                         in JobConfigScheduleDelegates.ScheduleTaskStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                         BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSystem.ConfigureJobTriggeredBy(this,
                                                      taskStream,
                                                      scheduleJobFunction,
                                                      batchStrategy);
        }
        
        public IJobConfigRequirements ConfigureJobTriggeredBy(EntityQuery entityQuery,
                                                              JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                              BatchStrategy batchStrategy)
        {
            return TaskSystem.ConfigureJobTriggeredBy(this,
                                                      entityQuery,
                                                      scheduleJobFunction,
                                                      batchStrategy);
        }


        //TODO: #73 - Implement other job types
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
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
