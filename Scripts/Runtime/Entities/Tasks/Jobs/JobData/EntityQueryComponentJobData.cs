using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class EntityQueryComponentJobData<T> : AbstractJobData
        where T : struct, IComponentData
    {
        private readonly EntityQueryComponentJobConfig<T> m_JobConfig;
        
        internal EntityQueryComponentJobData(EntityQueryComponentJobConfig<T> jobConfig, World world, byte context) : base(world, context, jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
