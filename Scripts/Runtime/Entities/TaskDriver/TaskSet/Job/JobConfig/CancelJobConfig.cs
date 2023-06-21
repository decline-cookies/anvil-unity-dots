using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public CancelJobConfig(ITaskSetOwner taskSetOwner, EntityProxyDataStream<TInstance> activeCancelDataStream)
            : base(taskSetOwner)
        {
            RequireDataStreamForCancelling(activeCancelDataStream);
            RequireCancelProgressLookup(taskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireCancelProgressLookup(ActiveLookupData<EntityProxyInstanceID> cancelProgressLookupData)
        {
            AddAccessWrapper(new CancelProgressLookupAccessWrapper(cancelProgressLookupData, AccessType.ExclusiveWrite, Usage.Cancelling));
        }

        private void RequireDataStreamForCancelling(EntityProxyDataStream<TInstance> activeCancelDataStream)
        {
            //When cancelling, we need to read from the pending cancel Active and write to the Pending for that type
            AddAccessWrapper(
                new DataStreamActiveCancelAccessWrapper<TInstance>(
                    activeCancelDataStream,
                    AccessType.SharedRead,
                    Usage.Cancelling));

            AddAccessWrapper(
                new DataStreamPendingAccessWrapper<TInstance>(
                    activeCancelDataStream,
                    AccessType.SharedWrite,
                    Usage.Cancelling));
        }
    }
}