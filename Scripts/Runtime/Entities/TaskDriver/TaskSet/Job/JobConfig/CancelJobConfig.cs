using Anvil.Unity.DOTS.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //TODO: Make sure we get the right iteration array for cancelled instances
        public CancelJobConfig(ITaskSetOwner taskSetOwner,
                               EntityProxyDataStream<TInstance> pendingCancelDataStream)
            : base(taskSetOwner)
        {
            RequireDataStreamForCancelling(pendingCancelDataStream);
            //TODO: Implement
            // RequireCancelProgressLookup(owningTaskSet.CancelProgressLookup);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireCancelProgressLookup(AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> cancelProgressLookup)
        {
            AddAccessWrapper(new CancelProgressLookupAccessWrapper(cancelProgressLookup, AccessType.ExclusiveWrite, Usage.Cancelling));
        }
        
        private void RequireDataStreamForCancelling(EntityProxyDataStream<TInstance> pendingCancelDataStream)
        {
            //When cancelling, we need to read from the pending cancel Active and write to the Pending
            AddAccessWrapper(new DataStreamPendingCancelActiveAccessWrapper<TInstance>(pendingCancelDataStream, AccessType.SharedRead, Usage.Cancelling));
            AddAccessWrapper(new DataStreamPendingAccessWrapper<TInstance>(pendingCancelDataStream, AccessType.SharedWrite, Usage.Cancelling));
        }
    }
}
