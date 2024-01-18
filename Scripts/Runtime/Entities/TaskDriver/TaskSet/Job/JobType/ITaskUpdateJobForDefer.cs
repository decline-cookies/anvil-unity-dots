using Anvil.Unity.DOTS.Data;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// A replacement for <see cref="IJobFor"/> when the number of work items is not known at Schedule time
    /// and you are using a <see cref="DeferredNativeArray{T}"/>
    /// This is specific to a context where the data is being updated.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/> data</typeparam>
    [JobProducerType(typeof(TaskUpdateJobForDeferExtension.WrapperJobProducer<>))]
    public interface ITaskUpdateJobForDefer
    {
        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        void InitForThread(int nativeThreadIndex);

        /// <summary>
        /// This method is called for each instance that is to be updated, allowing for the update work to
        /// occur.
        /// The <see cref="DataStreamUpdater{TInstance}"/> can be used to continue working on updating
        /// for this same instance next frame or resolve to a completed state.
        /// </summary>
        /// <param name="instance">The <see cref="IEntityKeyedTask"/> to update.</param>
        /// <param name="updater">A helper struct to continue or resolve</param>
        void Execute(int index);
    }
}