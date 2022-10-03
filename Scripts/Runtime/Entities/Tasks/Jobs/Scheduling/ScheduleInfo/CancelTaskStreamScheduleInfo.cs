using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelTaskStreamScheduleInfo<TInstance> : AbstractScheduleInfo,
                                                             ICancelTaskStreamScheduleInfo<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> m_ScheduleJobFunction;
        private readonly CancelJobData<TInstance> m_JobData;

        public int BatchSize { get; }
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }

        public DataStreamCancellationUpdater<TInstance> CancellationUpdater
        {
            get => m_JobData.GetDataStreamCancellationUpdater();
        }

        public CancelTaskStreamScheduleInfo(ProxyDataStream<TInstance> dataStream,
                                            BatchStrategy batchStrategy,
                                            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                            CancelJobData<TInstance> jobData) : base(scheduleJobFunction.Method)
        {
            m_ScheduleJobFunction = scheduleJobFunction;
            m_JobData = jobData;
            
            DeferredNativeArrayScheduleInfo = dataStream.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }

        public sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
