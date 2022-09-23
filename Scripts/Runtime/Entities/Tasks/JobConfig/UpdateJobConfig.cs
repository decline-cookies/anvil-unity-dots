using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class UpdateJobConfig<TInstance> : AbstractJobConfig,
                                                IUpdateJobConfig
        where TInstance : unmanaged, IProxyInstance
    {

        private readonly IUpdateJobConfig.ScheduleJobDelegate<TInstance> m_ScheduleJobFunction;
        private readonly UpdateTaskStreamScheduleInfo<TInstance> m_ScheduleInfo;

        public UpdateJobConfig(TaskFlowGraph taskFlowGraph, 
                               ITaskSystem taskSystem, 
                               ITaskDriver taskDriver,
                               IUpdateJobConfig.ScheduleJobDelegate<TInstance> scheduleJobFunction,
                               ITaskStream<TInstance> taskStream,
                               BatchStrategy batchStrategy,
                               RequestCancelDataStream requestCancelDataStream) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_ScheduleJobFunction = scheduleJobFunction;
            m_ScheduleInfo = new UpdateTaskStreamScheduleInfo<TInstance>(taskStream.DataStream, batchStrategy);
            
            RequireDataStreamForUpdate(taskStream, requestCancelDataStream);
        }

        protected sealed override JobHandle CallScheduleFunction(JobHandle dependsOn, 
                                                                 JobData jobData)
        {
            RequestCancelReader requestCancelReader = jobData.GetRequestCancelReader();
            m_ScheduleInfo.Updater = jobData.GetDataStreamUpdater<TInstance>(requestCancelReader);
            return m_ScheduleJobFunction(dependsOn, jobData, m_ScheduleInfo);
        }
    }
}
