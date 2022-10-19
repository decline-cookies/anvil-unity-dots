using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsAccessWrapper : AbstractAccessWrapper
    {
        public CancelRequestsDataStream CancelRequestsDataStream { get; }
        
        public byte Context { get; }
        

        public CancelRequestsAccessWrapper(CancelRequestsDataStream cancelRequestsDataStream, AccessType accessType, AbstractJobConfig.Usage usage, byte context) : base(accessType, usage)
        {
            CancelRequestsDataStream = cancelRequestsDataStream;
            Context = context;
        }

        public sealed override JobHandle Acquire()
        {
            return CancelRequestsDataStream.AccessController.AcquireAsync(AccessType);
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            CancelRequestsDataStream.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
