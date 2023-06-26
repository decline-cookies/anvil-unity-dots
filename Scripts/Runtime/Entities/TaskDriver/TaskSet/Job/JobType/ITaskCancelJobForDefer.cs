using Anvil.Unity.DOTS.Data;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// A replacement for <see cref="IJobFor"/> when the number of work items is not known at Schedule time
    /// and you are using a <see cref="DeferredNativeArray{T}"/>
    /// This is specific to a context where the data is being cancelled.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/> data</typeparam>
    [JobProducerType(typeof(TaskCancelJobForDeferExtension.WrapperJobStruct<,>))]
    public interface ITaskCancelJobForDefer<TInstance>
        where TInstance : unmanaged, IEntityKeyedTask
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);

        /// <summary>
        /// This method is called for each instance that was requested to be cancelled, allowing for the cancel work to
        /// occur.
        /// The <see cref="DataStreamCancellationUpdater{TInstance}"/> can be used to continue working on cancelling
        /// for this same instance next frame or resolve to cancel completed state.
        /// </summary>
        /// <param name="cancelInstance">The <see cref="IEntityKeyedTask"/> to cancel.</param>
        /// <param name="cancellationUpdater">A helper struct to continue or resolve</param>
        void Execute(TInstance cancelInstance, ref DataStreamCancellationUpdater<TInstance> cancellationUpdater);
    }
}