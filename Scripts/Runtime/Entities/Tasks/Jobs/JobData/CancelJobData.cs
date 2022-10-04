using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Specific <see cref="AbstractJobData"/> for use when cancelling instances of data in a data stream.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/></typeparam>
    public class CancelJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IEntityProxyInstance
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
            EntityProxyDataStream<TInstance> dataStream = m_CancelJobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.Cancelling);
            DataStreamCancellationUpdater<TInstance> cancellationUpdater = dataStream.CreateDataStreamCancellationUpdater(m_CancelJobConfig.GetDataStreamChannelResolver());
            return cancellationUpdater;
        }
    }
}
