using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsPendingAccessWrapper : AbstractAccessWrapper
    {
        public CancelRequestsDataStream CancelRequestsDataStream { get; }
        public CancelRequestsPendingAccessWrapper(CancelRequestsDataStream cancelRequestsDataStream,
                                                  AccessType accessType,
                                                  AbstractJobConfig.Usage usage)
            : base(accessType, usage)
        {
            CancelRequestsDataStream = cancelRequestsDataStream;
        }

        public override JobHandle Acquire()
        {
            return CancelRequestsDataStream.AcquirePendingAsync(AccessType);
        }

        public override void Release(JobHandle dependsOn)
        {
            CancelRequestsDataStream.ReleasePendingAsync(dependsOn);
        }
    }
}
