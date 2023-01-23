using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific scheduling information for a <see cref="CancelCompleteJobConfig"/>
    /// </summary>
    public class CancelCompleteScheduleInfo : AbstractScheduleInfo
    {
        private readonly CancelCompleteJobData m_JobData;
        private readonly JobConfigScheduleDelegates.ScheduleCancelCompleteJobDelegate m_ScheduleJobFunction;
        
        /// <summary>
        /// The scheduling information for the <see cref="DeferredNativeArray{T}"/> used in this type of job.
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }
        
        internal CancelCompleteScheduleInfo(CancelCompleteJobData jobData,
                                            CancelCompleteDataStream cancelCompleteDataStream,
                                            BatchStrategy batchStrategy,
                                            JobConfigScheduleDelegates.ScheduleCancelCompleteJobDelegate scheduleJobFunction)
        :base(scheduleJobFunction.Method, 
              batchStrategy, 
              CancelCompleteDataStream.MAX_ELEMENTS_PER_CHUNK)
        {
            m_JobData = jobData;
            m_ScheduleJobFunction = scheduleJobFunction;

            DeferredNativeArrayScheduleInfo = cancelCompleteDataStream.ScheduleInfo;
        }

        internal override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
