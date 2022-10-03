using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [JobProducerType(typeof(TaskCancelJobForDeferExtension.WrapperJobStruct<,>))]
    public interface ITaskCancelJobForDefer<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        void InitForThread(int nativeThreadIndex);

        //TODO: DOCS
        void Execute(TInstance pendingCancelInstance, ref DataStreamCancellationUpdater<TInstance> cancellationUpdater);
    }
}
