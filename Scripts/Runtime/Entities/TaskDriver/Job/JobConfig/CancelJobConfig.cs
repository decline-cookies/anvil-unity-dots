using Anvil.Unity.DOTS.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //TODO: Make sure we get the right iteration array for cancelled instances
        public CancelJobConfig(TaskFlowGraph taskFlowGraph,
                               AbstractWorkload owningWorkload,
                               PendingCancelDataStream<TInstance> pendingCancelDataStream)
            : base(taskFlowGraph,
                   owningWorkload)
        {
            RequireDataStreamForCancelling(pendingCancelDataStream);
            RequireCancelProgressLookup(owningWorkload.CancelProgressLookup);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireCancelProgressLookup(AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> cancelProgressLookup)
        {
            AddAccessWrapper(new CancelProgressLookupAccessWrapper(cancelProgressLookup, AccessType.ExclusiveWrite, Usage.Cancelling));
        }
        
        private void RequireDataStreamForCancelling(PendingCancelDataStream<TInstance> pendingCancelDataStream)
        {
            AddAccessWrapper(new PendingCancelDataStreamAccessWrapper<TInstance>(pendingCancelDataStream, AccessType.ExclusiveWrite, Usage.Cancelling));
        }
    }
}
