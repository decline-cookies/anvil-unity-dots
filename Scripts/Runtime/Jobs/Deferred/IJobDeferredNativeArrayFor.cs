using Anvil.Unity.DOTS.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A replacement for <see cref="IJobFor"/> when the number of work items is not known at Schedule time
    /// and you are using a <see cref="DeferredNativeArray{T}"/>
    /// </summary>
    [JobProducerType(typeof(JobDeferredNativeArrayForExtension.JobDeferredNativeArrayForProducer<>))]
    public interface IJobDeferredNativeArrayFor
    {
        /// <summary>
        /// Implement this method to perform work against a specific iteration index.
        /// </summary>
        /// <param name="index">The index of the <see cref="NativeArray{T}"/> from a <see cref="DeferredNativeArray{T}"/></param>
        void Execute(int index);
    }
}
