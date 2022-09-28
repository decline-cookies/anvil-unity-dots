using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class UpdateJobConfig<TInstance> : AbstractUpdatableJobConfig
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly JobConfigDelegates.ScheduleUpdateJobDelegate<TInstance> m_ScheduleJobFunction;
        private readonly UpdateTaskStreamScheduleInfo<TInstance> m_ScheduleInfo;

        public UpdateJobConfig(TaskFlowGraph taskFlowGraph,
                               ITaskSystem taskSystem,
                               ITaskDriver taskDriver,
                               JobConfigDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                               ITaskStream<TInstance> taskStream,
                               BatchStrategy batchStrategy,
                               CancelRequestsDataStream cancelRequestsDataStream) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_ScheduleJobFunction = scheduleJobFunction;
            ScheduleInfo = m_ScheduleInfo = new UpdateTaskStreamScheduleInfo<TInstance>(taskStream.DataStream, batchStrategy);

            RequireDataStreamForUpdate(taskStream, cancelRequestsDataStream);
        }

        protected sealed override string GetScheduleJobFunctionDebugInfo()
        {
            return $"{m_ScheduleJobFunction.Method.DeclaringType?.Name}.{m_ScheduleJobFunction.Method.Name}";
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - CANCELLATION
        //*************************************************************************************************************

        private void RequireRequestCancelDataStreamForRead(CancelRequestsDataStream cancelRequestsDataStream)
        {
            AddAccessWrapper(new JobConfigDataID(cancelRequestsDataStream, Usage.Read),
                             new CancelRequestsAccessWrapper(cancelRequestsDataStream, AccessType.SharedRead, byte.MaxValue));
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForUpdate(ITaskStream<TInstance> taskStream, CancelRequestsDataStream cancelRequestsDataStream)
        {
            AddAccessWrapper(new JobConfigDataID(taskStream.DataStream, Usage.Update),
                             new DataStreamAccessWrapper(taskStream.DataStream, AccessType.ExclusiveWrite));

            RequireRequestCancelDataStreamForRead(cancelRequestsDataStream);

            if (taskStream is not CancellableTaskStream<TInstance> cancellableTaskStream)
            {
                return;
            }

            RequireDataStreamForWrite(cancellableTaskStream.PendingCancelDataStream, Usage.WritePendingCancel);
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        protected sealed override JobHandle CallScheduleFunction(JobHandle dependsOn,
                                                                 JobData jobData)
        {
            CancelRequestsReader cancelRequestsReader = jobData.GetCancelRequestsReader();
            m_ScheduleInfo.SetUpdater(jobData.GetDataStreamUpdater<TInstance>(cancelRequestsReader));
            return m_ScheduleJobFunction(dependsOn, jobData, m_ScheduleInfo);
        }
    }
}
