using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: Does this need to exist?
    internal class CancelRequestsAccessWrapper : AbstractAccessWrapper
    {
        public CancelRequestDataStream CancelRequestDataStream { get; }
        
        public byte Context { get; }
        

        public CancelRequestsAccessWrapper(CancelRequestDataStream cancelRequestDataStream, AccessType accessType, AbstractJobConfig.Usage usage, byte context) : base(accessType, usage)
        {
            CancelRequestDataStream = cancelRequestDataStream;
            Context = context;
        }

        public sealed override JobHandle Acquire()
        {
            return CancelRequestDataStream.AccessController.AcquireAsync(AccessType);
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            CancelRequestDataStream.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
