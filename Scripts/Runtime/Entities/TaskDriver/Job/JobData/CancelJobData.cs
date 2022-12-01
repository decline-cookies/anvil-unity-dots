using Unity.Collections.LowLevel.Unsafe;
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

        internal CancelJobData(CancelJobConfig<TInstance> jobConfig) : base(jobConfig)
        {
            m_CancelJobConfig = jobConfig;
        }

        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        internal DataStreamCancellationUpdater<TInstance> GetDataStreamCancellationUpdater()
        {
            PendingCancelDataStream<TInstance> pendingCancelDataStream = m_CancelJobConfig.GetPendingCancelDataStream<TInstance>(AbstractJobConfig.Usage.Cancelling);
            DataStreamTargetResolver dataStreamTargetResolver = m_CancelJobConfig.GetDataStreamTargetResolver();
            UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelProgressLookup = m_CancelJobConfig.GetCancelProgressLookup(AbstractJobConfig.Usage.Cancelling);
            DataStreamCancellationUpdater<TInstance> cancellationUpdater = pendingCancelDataStream.CreateDataStreamCancellationUpdater(dataStreamTargetResolver, cancelProgressLookup);
            return cancellationUpdater;
        }
    }
}
