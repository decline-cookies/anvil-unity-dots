using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Triggering specific <see cref="AbstractJobData"/> for use when triggering the job based on an <see cref="EntityQuery"/>
    /// and requiring the <see cref="Entity"/>
    /// </summary>
    public class EntityQueryJobData : AbstractJobData
    {
        private readonly EntityQueryJobConfig m_JobConfig;
        
        internal EntityQueryJobData(EntityQueryJobConfig jobConfig) : base(jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
