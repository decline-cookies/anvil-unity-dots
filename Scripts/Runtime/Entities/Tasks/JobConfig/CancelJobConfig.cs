using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    //TODO: Abstract with UpdateJobConfig
    internal class CancelJobConfig<TInstance> : AbstractUpdatableJobConfig
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly JobConfigDelegates.ScheduleCancelJobDelegate<TInstance> m_ScheduleJobFunction;
        private readonly CancelTaskStreamScheduleInfo<TInstance> m_ScheduleInfo;
        

        public CancelJobConfig(TaskFlowGraph taskFlowGraph,
                               ITaskSystem taskSystem,
                               ITaskDriver taskDriver,
                               JobConfigDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                               CancellableTaskStream<TInstance> taskStream,
                               BatchStrategy batchStrategy) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_ScheduleJobFunction = scheduleJobFunction;
            ScheduleInfo = m_ScheduleInfo = new CancelTaskStreamScheduleInfo<TInstance>(taskStream.PendingCancelDataStream, batchStrategy);

            RequireDataStreamForCancelling(taskStream);
        }

        protected override string GetScheduleJobFunctionDebugInfo()
        {
            return $"{m_ScheduleJobFunction.Method.DeclaringType?.Name}.{m_ScheduleJobFunction.Method.Name}";
        }
        
        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForCancelling(CancellableTaskStream<TInstance> taskStream)
        {
            AddAccessWrapper(new JobConfigDataID(taskStream.PendingCancelDataStream, Usage.Cancelling),
                             new DataStreamAccessWrapper(taskStream.PendingCancelDataStream, AccessType.ExclusiveWrite));
            
        }
        
        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        protected sealed override JobHandle CallScheduleFunction(JobHandle dependsOn,
                                                                 JobData jobData)
        {
            m_ScheduleInfo.SetCancellationUpdater(jobData.GetDataStreamCancellationUpdater<TInstance>());
            return m_ScheduleJobFunction(dependsOn, jobData, m_ScheduleInfo);
        }
    }
}
