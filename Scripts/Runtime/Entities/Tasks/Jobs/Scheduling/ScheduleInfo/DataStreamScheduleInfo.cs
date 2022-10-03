using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class DataStreamScheduleInfo<TInstance> : AbstractScheduleInfo,
                                                       IDeferredScheduleInfo
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly JobConfigScheduleDelegates.ScheduleDeferredJobDelegate m_ScheduleJobFunction;
        private readonly TaskStreamJobData<TInstance> m_JobData;

        public int BatchSize { get; }
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }
        
        public DataStreamScheduleInfo(ProxyDataStream<TInstance> dataStream, 
                                      BatchStrategy batchStrategy,
                                      JobConfigScheduleDelegates.ScheduleDeferredJobDelegate scheduleJobFunction,
                                      TaskStreamJobData<TInstance> jobData) : base(scheduleJobFunction.Method)
        {
            DeferredNativeArrayScheduleInfo = dataStream.ScheduleInfo;
            m_JobData = jobData;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;

            m_ScheduleJobFunction = scheduleJobFunction;
        }

        public sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
