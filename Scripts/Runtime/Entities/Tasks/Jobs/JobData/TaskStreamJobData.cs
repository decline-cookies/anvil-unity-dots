using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class TaskStreamJobData<TInstance> : AbstractJobData
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly TaskStreamJobConfig<TInstance> m_JobConfig;

        internal TaskStreamJobData(TaskStreamJobConfig<TInstance> jobConfig, World world, byte context) : base(world, context, jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
