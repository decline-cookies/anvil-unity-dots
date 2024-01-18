using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Cancelling specific <see cref="AbstractJobData"/> for use when cancelling instances of data in a data stream.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/></typeparam>
    public class CancelJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IEntityKeyedTask
    {
        private readonly CancelJobConfig<TInstance> m_CancelJobConfig;

        internal CancelJobData(CancelJobConfig<TInstance> jobConfig) : base(jobConfig)
        {
            m_CancelJobConfig = jobConfig;
        }

        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        /// <summary>
        /// Fulfills an instance of the provided type for the job.
        /// </summary>
        /// <param name="cancellationUpdater">The <see cref="DataStreamCancellationUpdater{TInstance}"/></param>
        public void Fulfill(out DataStreamCancellationUpdater<TInstance> cancellationUpdater)
        {
            EntityProxyDataStream<TInstance> activeCancelDataStream = m_CancelJobConfig.GetActiveCancelDataStream<TInstance>();
            ResolveTargetTypeLookup resolveTargetTypeLookup = m_CancelJobConfig.GetResolveTargetTypeLookup();
            UnsafeParallelHashMap<EntityKeyedTaskID, bool> cancelProgressLookup = m_CancelJobConfig.GetCancelProgressLookup();
            cancellationUpdater = activeCancelDataStream.CreateDataStreamCancellationUpdater(resolveTargetTypeLookup, cancelProgressLookup);
        }
    }
}
