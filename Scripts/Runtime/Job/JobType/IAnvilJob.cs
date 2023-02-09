using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A replacement for <see cref="IJob"/> that calls an initialization function per thread.
    /// </summary>
    [JobProducerType(typeof(AnvilJobExtension.WrapperJobStruct<>))]
    public interface IAnvilJob
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);

        /// <summary>
        /// Implement this method to perform work.
        /// </summary>
        void Execute();
    }
}
