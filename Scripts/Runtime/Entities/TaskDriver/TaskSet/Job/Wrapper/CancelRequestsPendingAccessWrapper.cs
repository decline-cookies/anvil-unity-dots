using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelRequestsPendingAccessWrapper : AbstractDataStreamPendingAccessWrapper<CancelRequestsDataStream>
    {
        public CancelRequestsPendingAccessWrapper(
            CancelRequestsDataStream defaultStream,
            AccessType accessType,
            AbstractJobConfig.Usage usage)
            : base(defaultStream, accessType, usage) { }

        public override JobHandle AcquireAsync()
        {
            return m_DefaultStream.AcquirePendingAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            m_DefaultStream.ReleasePendingAsync(dependsOn);
        }
    }
}