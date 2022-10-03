using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class EntityQueryJobData : AbstractJobData
    {
        private readonly EntityQueryJobConfig m_JobConfig;
        
        internal EntityQueryJobData(EntityQueryJobConfig jobConfig, World world, byte context) : base(world, context, jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
