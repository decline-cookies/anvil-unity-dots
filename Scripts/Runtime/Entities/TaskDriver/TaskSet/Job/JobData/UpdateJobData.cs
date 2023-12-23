namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Updating specific <see cref="AbstractJobData"/> for use when updating instances of data in a data stream.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/></typeparam>
    public class UpdateJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IEntityKeyedTask
    {
        private readonly UpdateJobConfig<TInstance> m_UpdateJobConfig;

        internal UpdateJobData(UpdateJobConfig<TInstance> jobConfig) : base(jobConfig)
        {
            m_UpdateJobConfig = jobConfig;
        }

        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        /// <summary>
        /// Fulfills an instance of the provided type for the job.
        /// </summary>
        /// <param name="updater">The <see cref="DataStreamUpdater{TInstance}"/></param>
        public void Fulfill(out DataStreamUpdater<TInstance> updater)
        {
            EntityProxyDataStream<TInstance> dataStream = m_UpdateJobConfig.GetPendingDataStream<TInstance>(AbstractJobConfig.Usage.Update);
            ResolveTargetTypeLookup resolveTargetTypeLookup = m_UpdateJobConfig.GetResolveTargetTypeLookup();
            updater = dataStream.CreateDataStreamUpdater(resolveTargetTypeLookup);
        }
    }
}