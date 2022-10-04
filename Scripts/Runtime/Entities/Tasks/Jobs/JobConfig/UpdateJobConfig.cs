using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class UpdateJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IProxyInstance
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

            if (taskStream is not CancellableTaskStream<TInstance> cancellableTaskStream)
            {
                return;
            }
            
            RequireCancellableTaskStreamForWrite(cancellableTaskStream);
        }
        
        private void RequireRequestCancelDataStreamForRead(CancelRequestsDataStream cancelRequestsDataStream)
        {
            AddAccessWrapper(new JobConfigDataID(cancelRequestsDataStream, Usage.Read),
                             new CancelRequestsAccessWrapper(cancelRequestsDataStream, AccessType.SharedRead, byte.MaxValue));
        }
        
        private void RequireCancellableTaskStreamForWrite(CancellableTaskStream<TInstance> cancellableTaskStream)
        {
            RequireDataStreamForWrite(cancellableTaskStream.PendingCancelDataStream, Usage.WritePendingCancel);
        }
    }
}
