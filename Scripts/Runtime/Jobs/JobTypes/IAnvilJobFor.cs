using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A replacement for <see cref="IJobFor"/> that calls an initialization function per thread.
    /// </summary>
    [JobProducerType(typeof(AnvilJobForExtension.WrapperJobStruct<>))]
    public interface IAnvilJobFor
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);
        
        /// <summary>
        /// Implement this method to perform work at the specified index
        /// </summary>
        void Execute(int index);
    }
}
