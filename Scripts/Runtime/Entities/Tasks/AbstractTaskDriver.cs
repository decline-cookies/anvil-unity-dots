using Anvil.CSharp.Core;
using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriver<TTaskDriver, TTaskSystem> : AbstractAnvilBase,
                                                                         ITaskDriver
        where TTaskDriver : AbstractTaskDriver<TTaskDriver, TTaskSystem>
        where TTaskSystem : AbstractTaskSystem<TTaskDriver, TTaskSystem>

    {
        private readonly List<ITaskDriver> m_SubTaskDrivers;
        private readonly TaskFlowGraph m_TaskFlowGraph;
        private readonly CancelRequestsDataStream m_CancelRequestsDataStream;

        public TTaskSystem TaskSystem { get; }
        public byte Context { get; }
        

        protected AbstractTaskDriver(World world)
        {
            TaskSystem = world.GetOrCreateSystem<TTaskSystem>();
            Context = TaskSystem.RegisterTaskDriver((TTaskDriver)this);
            
            m_CancelRequestsDataStream = new CancelRequestsDataStream();

            m_SubTaskDrivers = new List<ITaskDriver>();

            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            m_TaskFlowGraph.CreateTaskStreams(TaskSystem, this);
            m_TaskFlowGraph.RegisterCancelRequestsDataStream(m_CancelRequestsDataStream, TaskSystem, this);
        }

        protected override void DisposeSelf()
        {
            foreach (ITaskDriver subTaskDriver in m_SubTaskDrivers)
            {
                subTaskDriver.Dispose();
            }

            m_SubTaskDrivers.Clear();

            m_TaskFlowGraph.DisposeFor(TaskSystem, this);
            base.DisposeSelf();
        }

        CancelRequestsDataStream ITaskDriver.GetCancelRequestsDataStream()
        {
            return m_CancelRequestsDataStream;
        }

        List<CancelRequestsDataStream> ITaskDriver.GetSubTaskDriverCancelRequests()
        {
            List<CancelRequestsDataStream> cancelRequestsDataStreams = new List<CancelRequestsDataStream>();
            foreach (ITaskDriver subTaskDriver in m_SubTaskDrivers)
            {
                cancelRequestsDataStreams.Add(subTaskDriver.GetCancelRequestsDataStream());
            }

            return cancelRequestsDataStreams;
        }

        ITaskSystem ITaskDriver.GetTaskSystem()
        {
            return TaskSystem;
        }

        public IJobConfigScheduling ConfigurePopulateJob(JobConfigDelegates.ScheduleJobDelegate scheduleJobFunction)
        {
            return TaskSystem.ConfigurePopulateJob(this,
                                                   scheduleJobFunction);
        }

        //TODO: Implement other job types
    }
}
