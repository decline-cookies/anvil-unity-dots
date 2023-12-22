using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific scheduling information for a <see cref="CancelJobConfig{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/> data</typeparam>
    public class CancelScheduleInfo<TInstance> : AbstractScheduleInfo
        where TInstance : unmanaged, IEntityKeyedTask
    {
        private readonly CancelJobData<TInstance> m_JobData;
        private readonly EntityProxyDataStream<TInstance> m_DataStream;
        private readonly JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> m_ScheduleJobFunction;
        private uint m_LastActiveCancelDataStreamVersion;


        /// <summary>
        /// The scheduling information for the <see cref="DeferredNativeArray{T}"/> used in this type of job.
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }

        internal CancelScheduleInfo(
            CancelJobData<TInstance> jobData,
            EntityProxyDataStream<TInstance> dataStream,
            BatchStrategy batchStrategy,
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction)
            : base(scheduleJobFunction.Method, batchStrategy, EntityProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK)
        {
            m_JobData = jobData;
            m_ScheduleJobFunction = scheduleJobFunction;
            m_DataStream = dataStream;

            DeferredNativeArrayScheduleInfo = m_DataStream.ActiveCancelScheduleInfo;
        }

        internal sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            dependsOn = m_ScheduleJobFunction(dependsOn, m_JobData, this);
            m_LastActiveCancelDataStreamVersion = m_DataStream.ActiveCancelDataVersion;

            return dependsOn;
        }

        internal override bool ShouldSchedule()
        {
            //If we've been written to, we need to schedule
            return m_DataStream.IsActiveCancelDataInvalidated(m_LastActiveCancelDataStreamVersion);
        }
    }
}