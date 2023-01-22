using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Triggering specific <see cref="AbstractJobData"/> for use when triggering the job based on a <see cref="NativeArray{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of data in the array</typeparam>
    public class NativeArrayJobData<T> : AbstractJobData
        where T : struct
    {
        internal NativeArrayJobData(NativeArrayJobConfig<T> jobConfig) : base(jobConfig)
        {
        }
    }
}
