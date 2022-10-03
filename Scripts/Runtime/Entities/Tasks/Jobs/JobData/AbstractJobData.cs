using Unity.Collections;
using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractJobData
    {
        private readonly byte m_Context;
        private readonly AbstractJobConfig m_JobConfig;

        public World World { get; }

        public ref readonly TimeData Time
        {
            get => ref World.Time;
        }


        protected AbstractJobData(World world,
                                  byte context,
                                  IJobConfig jobConfig)
        {
            World = world;
            m_Context = context;
            m_JobConfig = (AbstractJobConfig)jobConfig;
        }


        internal CancelRequestsReader GetCancelRequestsReader()
        {
            CancelRequestsDataStream cancelRequestsDataStream = m_JobConfig.GetCancelRequestsDataStream(AbstractJobConfig.Usage.Read);
            return cancelRequestsDataStream.CreateCancelRequestsReader();
        }

        public CancelRequestsWriter GetCancelRequestsWriter()
        {
            m_JobConfig.GetCancelRequestsDataStreamWithContext(AbstractJobConfig.Usage.Write, out CancelRequestsDataStream cancelRequestsDataStream, out byte context);
            //We want the context of who we're writing to, NOT our own context
            return cancelRequestsDataStream.CreateCancelRequestsWriter(context);
        }

        public DataStreamWriter<TInstance> GetDataStreamWriter<TInstance>()
            where TInstance : unmanaged, IProxyInstance
        {
            ProxyDataStream<TInstance> dataStream = m_JobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.Write);
            DataStreamWriter<TInstance> writer = dataStream.CreateDataStreamWriter(m_Context);
            return writer;
        }

        public DataStreamReader<TInstance> GetDataStreamReader<TInstance>()
            where TInstance : unmanaged, IProxyInstance
        {
            ProxyDataStream<TInstance> dataStream = m_JobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.Read);
            DataStreamReader<TInstance> reader = dataStream.CreateDataStreamReader();
            return reader;
        }

        //*************************************************************************************************************
        // NATIVE ARRAY
        //*************************************************************************************************************

        public NativeArray<T> GetNativeArrayReadWrite<T>()
            where T : unmanaged
        {
            return m_JobConfig.GetNativeArray<T>(AbstractJobConfig.Usage.Write);
        }

        public NativeArray<T> GetNativeArrayReadOnly<T>()
            where T : unmanaged
        {
            return m_JobConfig.GetNativeArray<T>(AbstractJobConfig.Usage.Read);
        }

        //*************************************************************************************************************
        // ENTITY QUERY
        //*************************************************************************************************************

        public NativeArray<Entity> GetEntityNativeArrayFromQuery()
        {
            return m_JobConfig.GetEntityNativeArrayFromQuery(AbstractJobConfig.Usage.Read);
        }
    }
}
