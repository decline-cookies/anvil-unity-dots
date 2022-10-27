using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class PendingCancelDataStreamAccessWrapper<T> : AbstractAccessWrapper
        where T : unmanaged, IEntityProxyInstance
    {
        public PendingCancelEntityProxyDataStream<T> PendingCancelDataStream { get; }

        public PendingCancelDataStreamAccessWrapper(PendingCancelEntityProxyDataStream<T> pendingCancelDataStream, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            PendingCancelDataStream = pendingCancelDataStream;
        }

        public sealed override JobHandle Acquire()
        {
            return PendingCancelDataStream.AccessController.AcquireAsync(AccessType);
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            PendingCancelDataStream.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
