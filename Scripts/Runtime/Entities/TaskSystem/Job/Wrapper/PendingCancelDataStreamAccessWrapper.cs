using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class PendingCancelDataStreamAccessWrapper<T> : AbstractAccessWrapper
        where T : unmanaged, IEntityProxyInstance
    {
        public CancelPendingDataStream<T> CancelPendingDataStream { get; }

        public PendingCancelDataStreamAccessWrapper(CancelPendingDataStream<T> cancelPendingDataStream, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            CancelPendingDataStream = cancelPendingDataStream;
        }

        public sealed override JobHandle Acquire()
        {
            return CancelPendingDataStream.AccessController.AcquireAsync(AccessType);
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            CancelPendingDataStream.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
