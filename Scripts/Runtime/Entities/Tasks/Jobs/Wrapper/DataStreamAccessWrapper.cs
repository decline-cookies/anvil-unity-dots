using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamAccessWrapper : IAccessWrapper
    {
        public AbstractProxyDataStream DataStream { get; }
        public AccessType AccessType { get; }

        public DataStreamAccessWrapper(AbstractProxyDataStream dataStream, AccessType accessType)
        {
            DataStream = dataStream;
            AccessType = accessType;
        }

        public void Dispose()
        {
            //Not needed
        }

        public JobHandle Acquire()
        {
            return DataStream.AccessController.AcquireAsync(AccessType);
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            DataStream.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
