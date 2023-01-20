using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific <see cref="AbstractJobData"/> for Jobs that have been triggered by the completion of cancelling
    /// instances in an <see cref="AbstractTaskDriver"/>.
    /// </summary>
    public class CancelCompleteJobData : AbstractJobData
    {
        private readonly CancelCompleteJobConfig m_JobConfig;

        internal CancelCompleteJobData(CancelCompleteJobConfig jobConfig) : base(jobConfig)
        {
            m_JobConfig = jobConfig;
        }

        /// <summary>
        /// Gets a <see cref="CancelCompleteReader"/> job-safe struct to use for reading <see cref="Entity"/>s
        /// that have completed their cancellation.
        /// </summary>
        /// <returns>The <see cref="CancelCompleteReader"/></returns>
        public CancelCompleteReader GetCancelCompleteReader()
        {
            CancelCompleteDataStream cancelCompleteDataStream = m_JobConfig.GetCancelCompleteDataStream();
            CancelCompleteReader cancelCompleteReader = cancelCompleteDataStream.CreateCancelCompleteReader();
            return cancelCompleteReader;
        }
    }
}
