using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Triggering specific <see cref="AbstractJobData"/> for use when triggering the job based on an <see cref="EntityQuery"/>
    /// that requires <see cref="IComponentData"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IComponentData"/></typeparam>
    public class EntityQueryComponentJobData<T> : AbstractJobData
        where T : unmanaged, IComponentData
    {
        private readonly EntityQueryComponentJobConfig<T> m_JobConfig;

        internal EntityQueryComponentJobData(EntityQueryComponentJobConfig<T> jobConfig) : base(jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
