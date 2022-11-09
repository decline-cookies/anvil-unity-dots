using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class PendingCancelDataStream<TInstance> : AbstractArrayDataStream<EntityProxyInstanceWrapper<TInstance>>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public PendingCancelDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
        }
        
        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.
        
        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

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
