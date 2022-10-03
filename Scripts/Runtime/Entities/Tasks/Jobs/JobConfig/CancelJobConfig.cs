using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class CancelJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IProxyInstance
    {
        public CancelJobConfig(TaskFlowGraph taskFlowGraph,
                               ITaskSystem taskSystem,
                               ITaskDriver taskDriver,
                               CancellableTaskStream<TInstance> taskStream)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireDataStreamForCancelling(taskStream);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForCancelling(CancellableTaskStream<TInstance> taskStream)
        {
            AddAccessWrapper(new JobConfigDataID(taskStream.PendingCancelDataStream, Usage.Cancelling),
                             new DataStreamAccessWrapper(taskStream.PendingCancelDataStream, AccessType.ExclusiveWrite));
        }
    }
}
