using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class CancelJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly CancelJobConfig<TInstance> m_CancelJobConfig;

        internal CancelJobData(CancelJobConfig<TInstance> jobConfig, World world, byte context) : base(world, context, jobConfig)
        {
            m_CancelJobConfig = jobConfig;
        }
        
        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************
        
        internal DataStreamCancellationUpdater<TInstance> GetDataStreamCancellationUpdater()
        {
            ProxyDataStream<TInstance> dataStream = m_CancelJobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.Cancelling);
            DataStreamCancellationUpdater<TInstance> cancellationUpdater = dataStream.CreateDataStreamCancellationUpdater(m_CancelJobConfig.GetDataStreamChannelResolver());
            return cancellationUpdater;
        }
    }
}
