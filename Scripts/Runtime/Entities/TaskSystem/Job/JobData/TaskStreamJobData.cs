using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Triggering specific <see cref="AbstractJobData"/> for use when triggering a job based on
    /// a <see cref="TaskStream{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> in
    /// the <see cref="TaskStream{TInstance}"/></typeparam>
    public class TaskStreamJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private readonly TaskStreamJobConfig<TInstance> m_JobConfig;

        internal TaskStreamJobData(TaskStreamJobConfig<TInstance> jobConfig, World world, byte context) : base(world, context, jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
