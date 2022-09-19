using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobData
    {
        private readonly JobConfig m_JobConfig;
        private readonly byte m_Context;

        public World World
        {
            get;
        }

        public ref readonly TimeData Time
        {
            get => ref World.Time;
        }

        
        internal JobData(World world,
                         byte context,
                         JobConfig jobConfig)
        {
            World = world;
            m_Context = context;
            m_JobConfig = jobConfig;
        }

        public DataStreamUpdater<TInstance> GetDataStreamUpdater<TInstance>()
            where TInstance : unmanaged, IProxyInstance
        {
            ProxyDataStream<TInstance> dataStream = m_JobConfig.GetDataStream<TInstance>(JobConfig.Usage.Update);
            DataStreamUpdater<TInstance> updater = dataStream.CreateDataStreamUpdater();
            return updater;
        }

        public DataStreamWriter<TInstance> GetDataStreamWriter<TInstance>()
            where TInstance : unmanaged, IProxyInstance
        {
            ProxyDataStream<TInstance> dataStream = m_JobConfig.GetDataStream<TInstance>(JobConfig.Usage.Write);
            DataStreamWriter<TInstance> writer = dataStream.CreateDataStreamWriter(m_Context);
            return writer;
        }

        public DataStreamReader<TInstance> GetDataStreamReader<TInstance>()
            where TInstance : unmanaged, IProxyInstance
        {
            ProxyDataStream<TInstance> dataStream = m_JobConfig.GetDataStream<TInstance>(JobConfig.Usage.Read);
            DataStreamReader<TInstance> reader = dataStream.CreateDataStreamReader();
            return reader;
        }
    }
}
