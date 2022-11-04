using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class CancelCompleteJobData : AbstractJobData
    {
        private readonly CancelCompleteJobConfig m_JobConfig;

        internal CancelCompleteJobData(CancelCompleteJobConfig jobConfig,
                                       World world,
                                       byte context) : base(world, context, jobConfig)
        {
            m_JobConfig = jobConfig;
        }

        public CancelCompleteReader GetCancelCompleteReader()
        {
            CancelCompleteDataStream cancelCompleteDataStream = m_JobConfig.GetCancelCompleteDataStream(AbstractJobConfig.Usage.Read);
            CancelCompleteReader cancelCompleteReader = cancelCompleteDataStream.CreateCancelCompleteReader();
            return cancelCompleteReader;
        }
    }
}
