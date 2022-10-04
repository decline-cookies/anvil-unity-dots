using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamAccessWrapper : AbstractAccessWrapper
    {
        public AbstractProxyDataStream DataStream { get; }

        public DataStreamAccessWrapper(AbstractProxyDataStream dataStream, AccessType accessType) : base(accessType)
        {
            DataStream = dataStream;
        }

        public sealed override JobHandle Acquire()
        {
            return DataStream.AccessController.AcquireAsync(AccessType);
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            DataStream.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
