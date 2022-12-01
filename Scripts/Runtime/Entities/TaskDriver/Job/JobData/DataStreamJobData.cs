using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Triggering specific <see cref="AbstractJobData"/> for use when triggering a job based on
    /// a <see cref="IAbstractDataStream{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> in
    /// the <see cref="IAbstractDataStream{TInstance}"/></typeparam>
    public class DataStreamJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private readonly DataStreamJobConfig<TInstance> m_JobConfig;

        internal DataStreamJobData(DataStreamJobConfig<TInstance> jobConfig, World world) : base(world, jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
