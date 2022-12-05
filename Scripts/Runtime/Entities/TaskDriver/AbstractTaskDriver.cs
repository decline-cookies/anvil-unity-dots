using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.VersionControl;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a context specific Task done via Jobs over a wide array of multiple instances of data.
    /// The goal of a TaskDriver is to convert specific data into general data that the corresponding
    /// <see cref="AbstractTaskDriverSystem"/> will process en mass and in parallel. The results of that general processing
    /// are then picked up by the TaskDriver to be converted to specific data again and passed on to a sub task driver
    /// or to another general system. 
    /// </summary>
    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
        private readonly CommonTaskSet m_CommonTaskSet;

        internal TaskSet TaskSet { get; }
        /// <summary>
        /// Reference to the associated <see cref="World"/>
        /// </summary>
        public World World { get; }

        internal List<AbstractTaskDriver> SubTaskDrivers { get; }
        
        internal AbstractTaskDriver Parent { get; }


        protected AbstractTaskDriver(World world) : this(world, null)
        {
        }
        
        //This constructor can also be called via Reflection
        private AbstractTaskDriver(World world, AbstractTaskDriver parent)
        {
            //We can't just pull this off the System because we might have triggered it's creation via
            //world.GetOrCreateSystem and it's OnCreate hasn't occured yet so it's World is still null.
            World = world;
            Parent = parent;

            

            
            //Create a new instance of ContextualTaskWork for this instance
            TaskSet = m_CommonTaskSet.CreateTaskSet(this, Parent?.TaskSet);

            SubTaskDrivers = new List<AbstractTaskDriver>();


            TaskDriverFactory.CreateSubTaskDrivers(this, SubTaskDrivers);

            HasCancellableData = TaskData.CancellableDataStreams.Count > 0
                              || SubTaskDrivers.Any(subTaskDriver => subTaskDriver.HasCancellableData)
                              || GoverningTaskSystem.HasCancellableData;

            //TODO: We can do this in hardening
            CancelFlow.BuildRequestData();
            
            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            //TODO: Investigate if we need this here: #66, #67, and/or #68 - https://github.com/decline-cookies/anvil-unity-dots/pull/87/files#r995032614
            m_TaskFlowGraph.RegisterTaskDriver(this);
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
            return $"{GetType().GetReadableName()}|{Context}";
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION
        //*************************************************************************************************************

        public IResolvableJobConfigRequirements ConfigureJobToCancel<TInstance>(ICommonCancellableDataStream<TInstance> dataStream,
                                                                                JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                                                BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return m_CommonTaskSet.ConfigureJobToCancel(dataStream,
                                                        scheduleJobFunction,
                                                        batchStrategy);
        }

        public IResolvableJobConfigRequirements ConfigureJobToUpdate<TInstance>(ICommonDataStream<TInstance> dataStream,
                                                                                JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                                BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return m_CommonTaskSet.ConfigureJobToUpdate(dataStream,
                                                        scheduleJobFunction,
                                                        batchStrategy);
        }
        

        //TODO: #101 - Should drivers have all the jobs or systems?
        public IJobConfigRequirements ConfigureJobTriggeredBy<TInstance>(IAbstractDataStream<TInstance> dataStream,
                                                                         in JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                         BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSet.ConfigureJobTriggeredBy((DataStream<TInstance>)dataStream,
                                                   scheduleJobFunction,
                                                   batchStrategy);
        }

        public IResolvableJobConfigRequirements ConfigureCancelJobFor<TInstance>(ICancellableDataStream<TInstance> dataStream,
                                                                                 in JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                                                 BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSet.ConfigureCancelJobFor((CancellableDataStream<TInstance>)dataStream,
                                                 scheduleJobFunction,
                                                 batchStrategy);
        }


        public IJobConfigRequirements ConfigureJobTriggeredBy(EntityQuery entityQuery,
                                                              JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                              BatchStrategy batchStrategy)
        {
            return TaskSet.ConfigureJobTriggeredBy(entityQuery,
                                                   scheduleJobFunction,
                                                   batchStrategy);
        }

        public IJobConfigRequirements ConfigureJobWhenCancelComplete(in JobConfigScheduleDelegates.ScheduleCancelCompleteJobDelegate scheduleJobFunction,
                                                                     BatchStrategy batchStrategy)
        {
            return TaskSet.ConfigureJobWhenCancelComplete(TaskSet.CancelCompleteDataStream,
                                                          scheduleJobFunction,
                                                          batchStrategy);
        }


        //TODO: #73 - Implement other job types
    }
}
