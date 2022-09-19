using Anvil.CSharp.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriver<TTaskDriver, TTaskSystem> : AbstractTaskDriver,
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
        
        private readonly TaskFlowGraph m_TaskFlowGraph;

        protected AbstractTaskDriver(World world)
        {
            TaskSystem = world.GetOrCreateSystem<TTaskSystem>();
            Context = TaskSystem.RegisterTaskDriver((TTaskDriver)this);

            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowDataSystem>().TaskFlowGraph;
            m_TaskFlowGraph.CreateDataStreams(TaskSystem, this);
        }

        protected override void DisposeSelf()
        {
            m_TaskFlowGraph.DisposeFor(this);
            base.DisposeSelf();
        }

        public JobConfig ConfigurePopulateJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                                    JobConfig.ScheduleJobDelegate scheduleJobFunction)
            where TInstance : unmanaged, IProxyInstance
        {
            return TaskSystem.ConfigurePopulateJobFor(this,
                                                      dataStream,
                                                      scheduleJobFunction);
        }
    }

    //TODO: Might be able to get rid of this
    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
    }
}
