using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobData
    {
        private readonly JobConfig m_JobConfig;

        public World World
        {
            get;
        }

        public ref readonly TimeData Time
        {
            get => ref World.Time;
        }

        public byte Context
        {
            get;
        }

        internal JobData(World world,
                         byte context,
                         JobConfig jobConfig)
        {
            World = world;
            Context = context;
            m_JobConfig = jobConfig;
        }

        public DataStreamUpdater<TInstance> GetDataStreamUpdater<TInstance>()
            where TInstance : unmanaged, IProxyInstance
        {
            ProxyDataStream<TInstance> dataStream = m_JobConfig.GetDataStream<TInstance>(JobConfig.Usage.Update);
            DataStreamUpdater<TInstance> updater = dataStream.CreateDataStreamUpdater(Context);
            return updater;
        }
    }
}
