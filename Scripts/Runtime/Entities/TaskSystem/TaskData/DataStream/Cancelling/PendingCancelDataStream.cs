using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class PendingCancelDataStream<TInstance> : AbstractArrayDataStream<EntityProxyInstanceWrapper<TInstance>>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        internal PendingCancelDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
        }

        internal DataStreamCancellationUpdater<TInstance> CreateDataStreamCancellationUpdater(DataStreamTargetResolver dataStreamTargetResolver,
                                                                                              UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelProgressLookup)
        {
            return new DataStreamCancellationUpdater<TInstance>(Pending.AsWriter(),
                                                                Live.AsDeferredJobArray(),
                                                                dataStreamTargetResolver,
                                                                cancelProgressLookup);
        }
    }
}
