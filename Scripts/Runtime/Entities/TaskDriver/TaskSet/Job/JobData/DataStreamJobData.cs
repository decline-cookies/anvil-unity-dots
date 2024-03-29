namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Triggering specific <see cref="AbstractJobData"/> for use when triggering a job based on
    /// a <see cref="IAbstractDataStream{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/> in
    /// the <see cref="IAbstractDataStream{TInstance}"/></typeparam>
    public class DataStreamJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IEntityKeyedTask
    {
        private readonly DataStreamJobConfig<TInstance> m_JobConfig;

        internal DataStreamJobData(DataStreamJobConfig<TInstance> jobConfig) : base(jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
