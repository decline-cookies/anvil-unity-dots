using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class NativeArrayJobData<T> : AbstractJobData
        where T : struct
    {
        private readonly NativeArrayJobConfig<T> m_JobConfig;

        internal NativeArrayJobData(NativeArrayJobConfig<T> jobConfig, World world, byte context) : base(world, context, jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
