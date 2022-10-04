using Anvil.CSharp.Core;
using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractTaskDriver<TTaskSystem> : AbstractTaskDriver
        where TTaskSystem : AbstractTaskSystem
    {
        public new TTaskSystem TaskSystem
        {
            get => (TTaskSystem)base.TaskSystem;
        }
        
        protected AbstractTaskDriver(World world) : base(world, world.GetOrCreateSystem<TTaskSystem>())
        {
        }
    }

    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
        private readonly List<AbstractTaskDriver> m_SubTaskDrivers;
        private readonly TaskFlowGraph m_TaskFlowGraph;
        
        public byte Context { get; }
        
        public AbstractTaskSystem TaskSystem { get; }
        internal CancelRequestsDataStream CancelRequestsDataStream { get; }

        protected AbstractTaskDriver(World world, AbstractTaskSystem abstractTaskSystem)
        {
            TaskSystem = abstractTaskSystem;
            
            Context = TaskSystem.RegisterTaskDriver(this);
            
            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            //TODO: Let the TaskFlowGraph create this for us.
            CancelRequestsDataStream = new CancelRequestsDataStream();
            
            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            m_TaskFlowGraph.CreateTaskStreams(TaskSystem, this);
            m_TaskFlowGraph.RegisterCancelRequestsDataStream(CancelRequestsDataStream, TaskSystem, this);
        }

        protected override void DisposeSelf()
        {
            //TODO: Let the Task Graph handle disposing this for us
            foreach (AbstractTaskDriver subTaskDriver in m_SubTaskDrivers)
            {
                subTaskDriver.Dispose();
            }
            m_SubTaskDrivers.Clear();
            
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
            where TInstance : unmanaged, IProxyInstance
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


        //TODO: Implement other job types
    }
}
