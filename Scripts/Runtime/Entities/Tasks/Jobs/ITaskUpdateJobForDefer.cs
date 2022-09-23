using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    [JobProducerType(typeof(TaskUpdateJobForDeferExtension.WrapperJobStruct<,>))]
    public interface ITaskUpdateJobForDefer<TInstance>
        where TInstance : unmanaged, IProxyInstance
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
