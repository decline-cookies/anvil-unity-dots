using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class DataStreamAccessWrapper : IAccessWrapper
    {
        public AbstractProxyDataStream DataStream
        {
            get;
        }

        public AccessType AccessType
        {
            get;
        }

        public DataStreamAccessWrapper(AbstractProxyDataStream dataStream, AccessType accessType)
        {
            DataStream = dataStream;
            AccessType = accessType;
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
