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

        private readonly TTaskDriver m_TypedThis;

        protected AbstractTaskDriver(World world)
        {
            m_TypedThis = (TTaskDriver)this;
            TaskSystem = world.GetOrCreateSystem<TTaskSystem>();
            Context = TaskSystem.RegisterTaskDriver(m_TypedThis);

            CreateProxyDataStreams();
        }

        protected override void DisposeSelf()
        {
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
