using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //TODO: Make sure we get the right iteration array for cancelled instances
        public CancelJobConfig(TaskFlowGraph taskFlowGraph,
                               AbstractTaskSystem taskSystem,
                               AbstractTaskDriver taskDriver,
                               CancelPendingDataStream<TInstance> cancelPendingDataStream)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireDataStreamForCancelling(cancelPendingDataStream);
            //TODO: need to make sure we get the corresponding progress lookup
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForCancelling(CancelPendingDataStream<TInstance> cancelPendingDataStream)
        {
            AddAccessWrapper(new PendingCancelDataStreamAccessWrapper<TInstance>(cancelPendingDataStream, AccessType.ExclusiveWrite, Usage.Cancelling));
        }
    }
}
