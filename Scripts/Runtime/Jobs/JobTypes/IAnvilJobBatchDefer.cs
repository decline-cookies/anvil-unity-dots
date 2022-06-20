using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A replacement for <see cref="IJobParallelForBatch"/> when the number of work items is not known
    /// at Schedule time and you are using a <see cref="DeferredNativeArray{T}"/>
    /// </summary>
    [JobProducerType(typeof(AnvilJobBatchDeferExtension.WrapperJobStruct<>))]
    public interface IAnvilJobBatchDefer
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);

        /// <summary>
        /// Implement this method to perform work against a batch
        /// </summary>
        /// <param name="startIndex">The start index of the <see cref="NativeArray{T}"/> from a <see cref="DeferredNativeArray{T}"/></param>
        /// <param name="count">The number of elements in this batch</param>
        void Execute(int startIndex, int count);
    }
}
