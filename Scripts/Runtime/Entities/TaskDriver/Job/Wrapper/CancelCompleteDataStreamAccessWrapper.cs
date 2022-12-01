using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteDataStreamAccessWrapper : AbstractAccessWrapper
    {
        public CancelCompleteDataStream CancelCompleteDataStream { get; }

        public CancelCompleteDataStreamAccessWrapper(CancelCompleteDataStream cancelCompleteDataStream,
                                                     AccessType accessType,
                                                     AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            CancelCompleteDataStream = cancelCompleteDataStream;
        }

        public override JobHandle Acquire()
        {
            return CancelCompleteDataStream.AccessController.AcquireAsync(AccessType);
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            CancelCompleteDataStream.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
