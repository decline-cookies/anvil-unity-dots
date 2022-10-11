using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using System.Collections.Generic;
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
        public new TTaskSystem TaskSystem
        {
            get => (TTaskSystem)base.TaskSystem;
        }
        
        protected AbstractTaskDriver(World world) : base(world, world.GetOrCreateSystem<TTaskSystem>())
        {
            Logger.Debug("Task Driver Constructor");
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
        
        /// <summary>
        /// The context associated with this TaskDriver. Will be unique to the corresponding
        /// <see cref="AbstractTaskSystem"/> and any other TaskDrivers of the same type.
        /// </summary>
        public byte Context { get; }
        
        /// <summary>
        /// Reference to the associated <see cref="AbstractTaskSystem"/>
        /// </summary>
        public AbstractTaskSystem TaskSystem { get; }
        
        internal CancelRequestsDataStream CancelRequestsDataStream { get; }

        protected AbstractTaskDriver(World world, AbstractTaskSystem abstractTaskSystem)
        {
            TaskSystem = abstractTaskSystem;
            
            Context = TaskSystem.RegisterTaskDriver(this);
            
            //TODO: #71 - Let the TaskFlowGraph create this for us.
            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            
            TaskStreamFactory.CreateTaskStreams(this);
            
            CancelRequestsDataStream = new CancelRequestsDataStream();
            
            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            //TODO: Register task streams
            m_TaskFlowGraph.RegisterCancelRequestsDataStream(CancelRequestsDataStream, TaskSystem, this);
        }

        protected override void DisposeSelf()
        {
            //TODO: #71 - Let the Task Graph handle disposing this for us
            m_SubTaskDrivers.DisposeAllAndTryClear();
            
            //TODO: Remove this dispose for
            //CancelRequestsDataStream is disposed by the TaskFlowGraph
            m_TaskFlowGraph.DisposeFor(TaskSystem, this);
            
            base.DisposeSelf();
        }

        internal List<CancelRequestsDataStream> GetSubTaskDriverCancelRequests()
        {
            List<CancelRequestsDataStream> cancelRequestsDataStreams = new List<CancelRequestsDataStream>();
            foreach (AbstractTaskDriver subTaskDriver in m_SubTaskDrivers)
            {
                cancelRequestsDataStreams.Add(subTaskDriver.CancelRequestsDataStream);
            }

            return cancelRequestsDataStreams;
        }
        
        public IJobConfigRequirements ConfigureJobTriggeredBy<TInstance>(TaskStream<TInstance> taskStream,
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
    }
}
