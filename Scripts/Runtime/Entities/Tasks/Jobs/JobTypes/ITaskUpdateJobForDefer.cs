using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    [JobProducerType(typeof(TaskUpdateJobForDeferExtension.WrapperJobStruct<,>))]
    public interface ITaskUpdateJobForDefer<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        void InitForThread(int nativeThreadIndex);

        //TODO: DOCS
        void Execute(ref TInstance instance, ref DataStreamUpdater<TInstance> updater);
    }
}
