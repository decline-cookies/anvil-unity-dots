using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Specific scheduling information for a <see cref="TaskStreamJobConfig{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IProxyInstance"/> data</typeparam>
    public class TaskStreamScheduleInfo<TInstance> : AbstractScheduleInfo
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly TaskStreamJobData<TInstance> m_JobData;
        private readonly JobConfigScheduleDelegates.ScheduleTaskStreamJobDelegate<TInstance> m_ScheduleJobFunction;

        /// <summary>
        /// The number of instances to process per batch.
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        /// The scheduling information for the <see cref="DeferredNativeArray{T}"/> used in this type of job.
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }

        internal TaskStreamScheduleInfo(TaskStreamJobData<TInstance> jobData,
                                        ProxyDataStream<TInstance> dataStream,
                                        BatchStrategy batchStrategy,
                                        JobConfigScheduleDelegates.ScheduleTaskStreamJobDelegate<TInstance> scheduleJobFunction) : base(scheduleJobFunction.Method)
        {
            m_JobData = jobData;
            m_ScheduleJobFunction = scheduleJobFunction;

            DeferredNativeArrayScheduleInfo = dataStream.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }

        internal sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
