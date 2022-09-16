using Anvil.CSharp.Core;
using System.Collections.Generic;
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

            CreateProxyDataStreams();
        }

        protected override void DisposeSelf()
        {
            m_TaskFlowGraph.DisposeFor(this);
            base.DisposeSelf();
        }

        private void CreateProxyDataStreams()
        {
            List<AbstractProxyDataStream> dataStreams = TaskDataStreamUtil.GenerateProxyDataStreamsOnType(this);
            foreach (AbstractProxyDataStream dataStream in dataStreams)
            {
                TaskSystem.RegisterTaskDriverDataStream(dataStream, this);
            }
        }

        public AbstractJobConfig ConfigurePopulateJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                                    JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                                    BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            return TaskSystem.ConfigurePopulateJobFor(dataStream,
                                                      scheduleJobFunction,
                                                      batchStrategy);
        }
    }

    //TODO: Might be able to get rid of this
    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
    }
}
