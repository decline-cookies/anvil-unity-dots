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
                               PendingCancelEntityProxyDataStream<TInstance> pendingCancelDataStream)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireDataStreamForCancelling(pendingCancelDataStream);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForCancelling(PendingCancelEntityProxyDataStream<TInstance> pendingCancelDataStream)
        {
            AddAccessWrapper(new PendingCancelDataStreamAccessWrapper<TInstance>(pendingCancelDataStream, AccessType.ExclusiveWrite, Usage.Cancelling));
        }
    }
}
