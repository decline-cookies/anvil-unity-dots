using Anvil.Unity.DOTS.Data;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// A replacement for <see cref="IJobFor"/> when the number of work items is not known at Schedule time
    /// and you are using a <see cref="DeferredNativeArray{T}"/>
    /// This is specific to a context where the data is being read.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data</typeparam>
    [JobProducerType(typeof(TaskJobForDeferExtension.WrapperJobStruct<,>))]
    public interface ITaskJobForDefer<in TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);

        /// <summary>
        /// This method is called for each instance that is being read, allowing for the work to
        /// occur.
        /// </summary>
        /// <param name="instance">The <see cref="IEntityProxyInstance"/> to read.</param>
        void Execute(TInstance instance);
    }
}
