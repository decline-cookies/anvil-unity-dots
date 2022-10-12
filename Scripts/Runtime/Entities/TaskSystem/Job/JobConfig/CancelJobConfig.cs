using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public CancelJobConfig(TaskFlowGraph taskFlowGraph,
                               AbstractTaskSystem taskSystem,
                               AbstractTaskDriver taskDriver,
                               TaskStream<TInstance> taskStream)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireDataStreamForCancelling(taskStream);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForCancelling(TaskStream<TInstance> taskStream)
        {
            Debug_EnsureCancellable(taskStream);
            AddAccessWrapper(new JobConfigDataID(taskStream.PendingCancelDataStream, Usage.Cancelling),
                             new DataStreamAccessWrapper(taskStream.PendingCancelDataStream, AccessType.ExclusiveWrite));
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureCancellable(TaskStream<TInstance> taskStream)
        {
            if (!taskStream.IsCancellable)
            {
                throw new NotSupportedException($"Tried to configure a cancel job for {taskStream} but it is not cancellable! Did you set the {nameof(CancellableAttribute)}?");
            }
        }
    }
}
