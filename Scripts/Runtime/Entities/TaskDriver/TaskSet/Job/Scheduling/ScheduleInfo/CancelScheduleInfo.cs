using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific scheduling information for a <see cref="CancelJobConfig{TInstance}"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data</typeparam>
    public class CancelScheduleInfo<TInstance> : AbstractScheduleInfo
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private readonly CancelJobData<TInstance> m_JobData;
        private readonly EntityProxyDataStream<TInstance> m_PendingCancelDataStream;
        private readonly JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> m_ScheduleJobFunction;
        private JobHandle m_LastReadHandle;


        /// <summary>
        /// The scheduling information for the <see cref="DeferredNativeArray{T}"/> used in this type of job.
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }

        internal DataStreamCancellationUpdater<TInstance> CancellationUpdater
        {
            get => m_JobData.GetDataStreamCancellationUpdater();
        }

        internal CancelScheduleInfo(
            CancelJobData<TInstance> jobData,
            EntityProxyDataStream<TInstance> pendingCancelDataStream,
            BatchStrategy batchStrategy,
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction)
            : base(scheduleJobFunction.Method, batchStrategy, EntityProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK)
        {
            m_JobData = jobData;
            m_ScheduleJobFunction = scheduleJobFunction;
            m_PendingCancelDataStream = pendingCancelDataStream;

            DeferredNativeArrayScheduleInfo = m_PendingCancelDataStream.PendingCancelScheduleInfo;
        }

        internal sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            m_LastReadHandle = m_PendingCancelDataStream.GetActiveDependencyFor(AccessType.SharedRead);
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }

        internal override bool ShouldSchedule()
        {
            //If we've been written to, we need to schedule
            return m_PendingCancelDataStream.IsActiveDataInvalidated(m_LastReadHandle);
        }
    }
}