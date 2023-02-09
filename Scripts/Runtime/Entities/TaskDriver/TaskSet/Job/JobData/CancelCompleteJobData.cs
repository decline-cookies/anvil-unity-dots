namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific <see cref="AbstractJobData"/> for Jobs that have been triggered by the completion of cancelling
    /// instances in an <see cref="AbstractTaskDriver"/>.
    /// </summary>
    public class CancelCompleteJobData : DataStreamJobData<CancelComplete>
    {
        private readonly CancelCompleteJobConfig m_JobConfig;

        internal CancelCompleteJobData(CancelCompleteJobConfig jobConfig) : base(jobConfig)
        {
            m_JobConfig = jobConfig;
        }
    }
}
