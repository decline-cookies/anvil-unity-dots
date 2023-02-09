using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelCompleteActiveAccessWrapper : AbstractAccessWrapper
    {
        public CancelCompleteDataStream CancelCompleteDataStream { get; }

        public CancelCompleteActiveAccessWrapper(
            CancelCompleteDataStream cancelCompleteDataStream,
            AccessType accessType,
            AbstractJobConfig.Usage usage)
            : base(accessType, usage)
        {
            CancelCompleteDataStream = cancelCompleteDataStream;
        }

        public override JobHandle AcquireAsync()
        {
            return CancelCompleteDataStream.AcquireActiveAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            CancelCompleteDataStream.ReleaseActiveAsync(dependsOn);
        }
    }
}