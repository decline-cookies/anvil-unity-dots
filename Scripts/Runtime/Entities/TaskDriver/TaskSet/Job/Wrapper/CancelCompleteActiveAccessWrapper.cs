using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteActiveAccessWrapper : AbstractAccessWrapper
    {
        public CancelCompleteDataStream CancelCompleteDataStream { get; }

        public CancelCompleteActiveAccessWrapper(CancelCompleteDataStream cancelCompleteDataStream,
                                                 AccessType accessType,
                                                 AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            CancelCompleteDataStream = cancelCompleteDataStream;
        }

        public override JobHandle Acquire()
        {
            return CancelCompleteDataStream.AcquireActiveAsync(AccessType);
        }

        public override void Release(JobHandle dependsOn)
        {
            CancelCompleteDataStream.ReleaseActiveAsync(dependsOn);
        }
    }
}
