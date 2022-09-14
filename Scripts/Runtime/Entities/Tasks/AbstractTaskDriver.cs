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

        private readonly TTaskDriver m_TypedThis;

        protected AbstractTaskDriver(World world)
        {
            m_TypedThis = (TTaskDriver)this;
            TaskSystem = world.GetOrCreateSystem<TTaskSystem>();
            Context = TaskSystem.RegisterTaskDriver(m_TypedThis);

            //TODO: Need to autogenerate the proxydatastreams and build up processors
        }

        protected override void DisposeSelf()
        {
            base.DisposeSelf();
        }

        public IJobConfig ConfigurePopulateJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                             JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                             BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            //TODO: Ensure this datastream is owned by the TaskDriver or the System
            return TaskSystem.ConfigurePopulateJobFor(dataStream,
                                                      scheduleJobFunction,
                                                      batchStrategy,
                                                      m_TypedThis);
        }
    }

    //TODO: Might be able to get rid of this
    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
    }
}
