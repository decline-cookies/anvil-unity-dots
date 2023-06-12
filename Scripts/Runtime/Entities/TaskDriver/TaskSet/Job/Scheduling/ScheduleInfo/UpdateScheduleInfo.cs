using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific scheduling information for a <see cref="UpdateJobConfig{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data</typeparam>
    public class UpdateScheduleInfo<TInstance> : AbstractScheduleInfo
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private readonly UpdateJobData<TInstance> m_JobData;
        private readonly EntityProxyDataStream<TInstance> m_DataStream;
        private readonly JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> m_ScheduleJobFunction;
        private JobHandle m_LastReadHandle;
        
        /// <summary>
        /// The scheduling information for the <see cref="DeferredNativeArray{T}"/> used in this type of job.
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }

        internal DataStreamUpdater<TInstance> Updater
        {
            get => m_JobData.GetDataStreamUpdater();
        }

        internal UpdateScheduleInfo(
            UpdateJobData<TInstance> jobData,
            EntityProxyDataStream<TInstance> dataStream,
            BatchStrategy batchStrategy,
            JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction)
            : base(scheduleJobFunction.Method, batchStrategy, EntityProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK)
        {
            m_JobData = jobData;
            m_ScheduleJobFunction = scheduleJobFunction;
            m_DataStream = dataStream;

            DeferredNativeArrayScheduleInfo = m_DataStream.ScheduleInfo;
        }

        internal override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            m_LastReadHandle = m_DataStream.GetActiveDependencyFor(AccessType.SharedRead);
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
        
        internal override bool ShouldSchedule()
        {
            //If we've been written to, we need to schedule
            return m_DataStream.IsActiveDataInvalidated(m_LastReadHandle);
        }
    }
}