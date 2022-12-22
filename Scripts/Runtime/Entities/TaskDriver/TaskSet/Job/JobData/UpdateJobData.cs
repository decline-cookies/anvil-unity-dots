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

        internal UpdateJobData(UpdateJobConfig<TInstance> jobConfig) : base(jobConfig)
        {
            m_UpdateJobConfig = jobConfig;
        }
        
        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        internal DataStreamUpdater<TInstance> GetDataStreamUpdater()
        {
            EntityProxyDataStream<TInstance> dataStream = m_UpdateJobConfig.GetPendingDataStream<TInstance>(AbstractJobConfig.Usage.Update);
            ResolveTargetTypeLookup resolveTargetTypeLookup = m_UpdateJobConfig.GetResolveTargetTypeLookup();
            DataStreamUpdater<TInstance> updater = dataStream.CreateDataStreamUpdater(resolveTargetTypeLookup);
            return updater;
        }
    }
}
