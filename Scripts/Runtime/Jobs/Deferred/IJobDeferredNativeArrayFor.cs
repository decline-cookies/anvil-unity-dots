using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    //TODO: Rename to Anvil and add other Anvil specifics so we can use the same Job structs
    /// <summary>
    /// A replacement for <see cref="IJobFor"/> when the number of work items is not known at Schedule time
    /// and you are using a <see cref="DeferredNativeArray{T}"/>
    /// </summary>
    [JobProducerType(typeof(JobDeferredNativeArrayForExtension.JobDeferredNativeArrayForProducer<>))]
    public interface IJobDeferredNativeArrayFor
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);
        
        /// <summary>
        /// Implement this method to perform work against a specific iteration index.
        /// </summary>
        /// <param name="index">The index of the <see cref="NativeArray{T}"/> from a <see cref="DeferredNativeArray{T}"/></param>
        void Execute(int index);
    }
}
