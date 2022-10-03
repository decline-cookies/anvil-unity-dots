using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class UpdateJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly UpdateJobConfig<TInstance> m_UpdateJobConfig;

        internal UpdateJobData(UpdateJobConfig<TInstance> jobConfig, World world, byte context) : base(world, context, jobConfig)
        {
            m_UpdateJobConfig = jobConfig;
        }
        
        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        internal DataStreamUpdater<TInstance> GetDataStreamUpdater(CancelRequestsReader cancelRequestsReader)
        {
            ProxyDataStream<TInstance> dataStream = m_UpdateJobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.Update);
            ProxyDataStream<TInstance> pendingCancelDataStream = m_UpdateJobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.WritePendingCancel);
            DataStreamUpdater<TInstance> updater = dataStream.CreateDataStreamUpdater(cancelRequestsReader,
                                                                                      pendingCancelDataStream,
                                                                                      m_UpdateJobConfig.GetDataStreamChannelResolver());
            return updater;
        }
    }
}
