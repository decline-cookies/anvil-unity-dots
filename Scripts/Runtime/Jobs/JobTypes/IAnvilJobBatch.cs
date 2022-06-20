using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A replacement for <see cref="IJobParallelForBatch"/> that calls an initialization function per thread.
    /// </summary>
    [JobProducerType(typeof(AnvilJobBatchExtension.WrapperJobStruct<>))]
    public interface IAnvilJobBatch
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);
        
        /// <summary>
        /// Implement this method to perform work for a certain subset of elements.
        /// </summary>
        /// <param name="startIndex">The start index to begin work on</param>
        /// <param name="count">The number of elements in this batch</param>
        void Execute(int startIndex, int count);
    }
}
