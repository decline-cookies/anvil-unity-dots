using Unity.Collections;
using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobData
    {
        private readonly AbstractJobConfig m_JobConfig;
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
                         AbstractJobConfig jobConfig)
        {
            World = world;
            m_Context = context;
            m_JobConfig = jobConfig;
        }
        
        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        internal DataStreamUpdater<TInstance> GetDataStreamUpdater<TInstance>(CancelRequestsReader cancelRequestsReader)
            where TInstance : unmanaged, IProxyInstance
        {
            ProxyDataStream<TInstance> dataStream = m_JobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.Update);
            ProxyDataStream<TInstance> pendingCancelDataStream = m_JobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.WritePendingCancel);
            DataStreamUpdater<TInstance> updater = dataStream.CreateDataStreamUpdater(cancelRequestsReader,
                                                                                      pendingCancelDataStream,
                                                                                      m_JobConfig.GetDataStreamChannelResolver());
            return updater;
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
