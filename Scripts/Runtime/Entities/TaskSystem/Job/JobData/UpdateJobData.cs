using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Updating specific <see cref="AbstractJobData"/> for use when updating instances of data in a data stream.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/></typeparam>
    public class UpdateJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IEntityProxyInstance
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
            EntityProxyDataStream<TInstance> dataStream = m_UpdateJobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.Update);
            EntityProxyDataStream<TInstance> pendingCancelDataStream = m_UpdateJobConfig.GetDataStream<TInstance>(AbstractJobConfig.Usage.WritePendingCancel);
            DataStreamUpdater<TInstance> updater = dataStream.CreateDataStreamUpdater(cancelRequestsReader,
                                                                                      pendingCancelDataStream,
                                                                                      m_UpdateJobConfig.GetDataStreamChannelResolver());
            return updater;
        }
    }
}
