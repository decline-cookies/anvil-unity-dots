using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Triggering specific <see cref="AbstractJobData"/> for use when triggering the job based on a <see cref="NativeArray{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of data in the array</typeparam>
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
