using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class UpdateTaskStreamScheduleInfo<TInstance> : AbstractScheduleInfo,
                                                             IUpdateTaskStreamScheduleInfo<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> m_ScheduleJobFunction;
        private readonly UpdateJobData<TInstance> m_JobData;

        public int BatchSize { get; }
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }

        public DataStreamUpdater<TInstance> Updater
        {
            get
            {
                CancelRequestsReader cancelRequestsReader = m_JobData.GetCancelRequestsReader();
                return m_JobData.GetDataStreamUpdater(cancelRequestsReader);
            }
        }

        public UpdateTaskStreamScheduleInfo(ProxyDataStream<TInstance> dataStream,
                                            BatchStrategy batchStrategy,
                                            JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                            UpdateJobData<TInstance> jobData) : base(scheduleJobFunction.Method)
        {
            m_ScheduleJobFunction = scheduleJobFunction;
            m_JobData = jobData;

            DeferredNativeArrayScheduleInfo = dataStream.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }

        public override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
