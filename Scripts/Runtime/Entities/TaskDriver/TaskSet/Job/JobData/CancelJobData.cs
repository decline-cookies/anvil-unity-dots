using Unity.Collections.LowLevel.Unsafe;

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
            EntityProxyDataStream<TInstance> pendingCancelDataStream = m_CancelJobConfig.GetPendingCancelDataStream<TInstance>();
            ResolveTargetTypeLookup resolveTargetTypeLookup = m_CancelJobConfig.GetResolveTargetTypeLookup();
            UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelProgressLookup = m_CancelJobConfig.GetCancelProgressLookup();
            DataStreamCancellationUpdater<TInstance> cancellationUpdater = pendingCancelDataStream.CreateDataStreamCancellationUpdater(resolveTargetTypeLookup, cancelProgressLookup);
            return cancellationUpdater;
        }
    }
}
