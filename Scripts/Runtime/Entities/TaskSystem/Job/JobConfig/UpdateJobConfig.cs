using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class UpdateJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public UpdateJobConfig(TaskFlowGraph taskFlowGraph,
                               AbstractTaskSystem taskSystem,
                               AbstractTaskDriver taskDriver,
                               TaskStream<TInstance> taskStream,
                               CancelRequestsDataStream cancelRequestsDataStream) 
            : base(taskFlowGraph, 
                   taskSystem, 
                   taskDriver)
        {
            RequireDataStreamForUpdate(taskStream, cancelRequestsDataStream);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForUpdate(TaskStream<TInstance> taskStream, CancelRequestsDataStream cancelRequestsDataStream)
        {
            AddAccessWrapper(new JobConfigDataID(taskStream.DataStream, Usage.Update),
                             new DataStreamAccessWrapper(taskStream.DataStream, AccessType.ExclusiveWrite));

            RequireRequestCancelDataStreamForRead(cancelRequestsDataStream);

            if (taskStream.IsCancellable)
            {
                RequireCancellableTaskStreamForWrite(taskStream);
            }
        }
        
        private void RequireRequestCancelDataStreamForRead(CancelRequestsDataStream cancelRequestsDataStream)
        {
            AddAccessWrapper(new JobConfigDataID(cancelRequestsDataStream, Usage.Read),
                             new CancelRequestsAccessWrapper(cancelRequestsDataStream, AccessType.SharedRead, byte.MaxValue));
        }
        
        private void RequireCancellableTaskStreamForWrite(TaskStream<TInstance> cancellableTaskStream)
        {
            Debug_EnsureIsCancellable(cancellableTaskStream);
            RequireDataStreamForWrite(cancellableTaskStream.PendingCancelDataStream, Usage.WritePendingCancel);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureIsCancellable(TaskStream<TInstance> cancellableTaskStream)
        {
            if (!cancellableTaskStream.IsCancellable || cancellableTaskStream.PendingCancelDataStream == null)
            {
                throw new InvalidOperationException($"{this} is trying register {cancellableTaskStream} as a cancellable task stream but it can't be cancelled!");
            }
        }
    }
}
