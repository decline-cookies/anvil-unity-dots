using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public CancelJobConfig(ITaskSetOwner taskSetOwner, EntityProxyDataStream<TInstance> pendingCancelDataStream)
            : base(taskSetOwner)
        {
            RequireDataStreamForCancelling(pendingCancelDataStream);
            RequireCancelProgressLookup(taskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireCancelProgressLookup(ActiveLookupData<EntityProxyInstanceID> cancelProgressLookupData)
        {
            AddAccessWrapper(new CancelProgressLookupAccessWrapper(cancelProgressLookupData, AccessType.ExclusiveWrite, Usage.Cancelling));
        }

        private void RequireDataStreamForCancelling(EntityProxyDataStream<TInstance> pendingCancelDataStream)
        {
            //When cancelling, we need to read from the pending cancel Active and write to the Pending for that type
            AddAccessWrapper(
                new DataStreamPendingCancelActiveAccessWrapper<TInstance>(
                    pendingCancelDataStream,
                    AccessType.SharedRead,
                    Usage.Cancelling));

            AddAccessWrapper(
                new DataStreamPendingAccessWrapper<TInstance>(
                    pendingCancelDataStream,
                    AccessType.SharedWrite,
                    Usage.Cancelling));
        }
    }
}