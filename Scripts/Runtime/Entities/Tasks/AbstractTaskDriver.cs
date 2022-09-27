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


        public TTaskSystem TaskSystem { get; }
        public byte Context { get; }
        internal CancelRequestsDataStream CancelRequestsDataStream { get; }


        protected AbstractTaskDriver(World world)
        {
            TaskSystem = world.GetOrCreateSystem<TTaskSystem>();
            Context = TaskSystem.RegisterTaskDriver((TTaskDriver)this);

            //TODO: Make sure the graph is aware
            CancelRequestsDataStream = new CancelRequestsDataStream();

            m_SubTaskDrivers = new List<ITaskDriver>();

            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            m_TaskFlowGraph.CreateTaskStreams(TaskSystem, this);
        }

        protected override void DisposeSelf()
        {
            foreach (ITaskDriver subTaskDriver in m_SubTaskDrivers)
            {
                subTaskDriver.Dispose();
            }

            m_SubTaskDrivers.Clear();

            //TODO: Once the graph is aware, this can go away
            CancelRequestsDataStream.Dispose();

            m_TaskFlowGraph.DisposeFor(TaskSystem, this);
            base.DisposeSelf();
        }

        CancelRequestsDataStream ITaskDriver.GetCancelRequestsDataStream()
        {
            return CancelRequestsDataStream;
        }

        public IJobConfigScheduling ConfigurePopulateJob(JobConfigDelegates.ScheduleJobDelegate scheduleJobFunction)
        {
            return TaskSystem.ConfigurePopulateJob(this,
                                                   scheduleJobFunction);
        }

        //TODO: Implement other job types
    }
}
