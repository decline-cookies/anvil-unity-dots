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
        public TTaskSystem TaskSystem
        {
            get;
        }

        public byte Context
        {
            get;
        }

        private readonly List<ITaskDriver> m_SubTaskDrivers;
        private readonly TaskFlowGraph m_TaskFlowGraph;

        protected AbstractTaskDriver(World world)
        {
            TaskSystem = world.GetOrCreateSystem<TTaskSystem>();
            Context = TaskSystem.RegisterTaskDriver((TTaskDriver)this);

            m_SubTaskDrivers = new List<ITaskDriver>();

            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            m_TaskFlowGraph.CreateDataStreams(TaskSystem, this);
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

        public IScheduleJobConfig ConfigurePopulateJob(IJobConfig.ScheduleJobDelegate scheduleJobFunction)
        {
            return TaskSystem.ConfigurePopulateJob(this,
                                                   scheduleJobFunction);
        }
        
        //TODO: Implement other job types
    }
}
