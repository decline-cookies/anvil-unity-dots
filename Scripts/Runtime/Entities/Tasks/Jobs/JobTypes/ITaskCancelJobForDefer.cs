using Anvil.Unity.DOTS.Data;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// A replacement for <see cref="IJobFor"/> when the number of work items is not known at Schedule time
    /// and you are using a <see cref="DeferredNativeArray{T}"/>
    /// This is specific to a context where the data is being cancelled.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data</typeparam>
    [JobProducerType(typeof(TaskCancelJobForDeferExtension.WrapperJobStruct<,>))]
    public interface ITaskCancelJobForDefer<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);

        /// <summary>
        /// Implement this method to cancel the passed instance.
        /// The <see cref="DataStreamCancellationUpdater{TInstance}"/> can be used to continue working on cancelling
        /// for this same instance next frame or resolve to cancel completed state.
        /// </summary>
        /// <param name="pendingCancelInstance">The <see cref="IEntityProxyInstance"/> to cancel.</param>
        /// <param name="cancellationUpdater">A helper struct to continue or resolve</param>
        void Execute(TInstance pendingCancelInstance, ref DataStreamCancellationUpdater<TInstance> cancellationUpdater);
    }
}
