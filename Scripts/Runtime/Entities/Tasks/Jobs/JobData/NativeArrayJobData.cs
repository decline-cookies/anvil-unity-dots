using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Specific <see cref="AbstractJobData"/> for use when triggering the job based on a <see cref="NativeArray{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
