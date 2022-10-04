using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Specific scheduling information for a <see cref="UpdateJobConfig{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data</typeparam>
    public class UpdateTaskStreamScheduleInfo<TInstance> : AbstractScheduleInfo
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private readonly UpdateJobData<TInstance> m_JobData;
        private readonly JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> m_ScheduleJobFunction;

        /// <summary>
        /// The number of instances to process per batch.
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        /// The scheduling information for the <see cref="DeferredNativeArray{T}"/> used in this type of job.
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }

        internal DataStreamUpdater<TInstance> Updater
        {
            get
            {
                CancelRequestsReader cancelRequestsReader = m_JobData.GetCancelRequestsReader();
                return m_JobData.GetDataStreamUpdater(cancelRequestsReader);
            }
        }

        internal UpdateTaskStreamScheduleInfo(UpdateJobData<TInstance> jobData,
                                              EntityProxyDataStream<TInstance> dataStream,
                                              BatchStrategy batchStrategy,
                                              JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction) : base(scheduleJobFunction.Method)
        {
            m_JobData = jobData;
            m_ScheduleJobFunction = scheduleJobFunction;
            
            DeferredNativeArrayScheduleInfo = dataStream.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? EntityProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }

        internal override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
