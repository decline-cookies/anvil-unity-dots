using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific scheduling information for a <see cref="DataStreamJobConfig{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data</typeparam>
    public class DataStreamScheduleInfo<TInstance> : AbstractScheduleInfo
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private readonly DataStreamJobData<TInstance> m_JobData;
        private readonly EntityProxyDataStream<TInstance> m_DataStream;
        private readonly JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> m_ScheduleJobFunction;

        /// <summary>
        /// The scheduling information for the <see cref="DeferredNativeArray{T}"/> used in this type of job.
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }

        internal DataStreamActiveReader<TInstance> Reader
        {
            get
            {
                m_JobData.Fulfill(out DataStreamActiveReader<TInstance> stream);
                return stream;
            }
        }

        internal DataStreamScheduleInfo(
            DataStreamJobData<TInstance> jobData,
            EntityProxyDataStream<TInstance> dataStream,
            BatchStrategy batchStrategy,
            JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction)
            : base(scheduleJobFunction.Method, batchStrategy, EntityProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK)
        {
            m_JobData = jobData;
            m_ScheduleJobFunction = scheduleJobFunction;
            m_DataStream = dataStream;

            DeferredNativeArrayScheduleInfo = m_DataStream.ScheduleInfo;
        }

        internal sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }

        internal sealed override bool ShouldSchedule()
        {
            //If we've been written to, we need to schedule
            return m_DataStream.IsDataInvalidated;
        }
    }
}