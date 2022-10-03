using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class CancelRequestsAccessWrapper : IAccessWrapper
    {
        public CancelRequestsDataStream CancelRequestsDataStream { get; }
        public AccessType AccessType { get; }
        public byte Context { get; }
        

        public CancelRequestsAccessWrapper(CancelRequestsDataStream cancelRequestsDataStream, AccessType accessType, byte context)
        {
            CancelRequestsDataStream = cancelRequestsDataStream;
            AccessType = accessType;
            Context = context;
        }
        
        public void Dispose()
        {
            //Not needed
        }

        public JobHandle Acquire()
        {
            return CancelRequestsDataStream.AccessController.AcquireAsync(AccessType);
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            CancelRequestsDataStream.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
