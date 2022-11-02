using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Cancelling specific <see cref="AbstractJobData"/> for use when cancelling instances of data in a data stream.
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
            throw new NotImplementedException();
            // CancelPendingDataStream<TInstance> cancelPendingDataStream = m_CancelJobConfig.GetPendingCancelDataStream<TInstance>(AbstractJobConfig.Usage.Cancelling);
            // DataStreamTargetResolver dataStreamTargetResolver = m_CancelJobConfig.GetDataStreamTargetResolver();
            // DataStreamCancellationUpdater<TInstance> cancellationUpdater = cancelPendingDataStream.CreateDataStreamCancellationUpdater(dataStreamTargetResolver);
            // return cancellationUpdater;
        }
    }
}
