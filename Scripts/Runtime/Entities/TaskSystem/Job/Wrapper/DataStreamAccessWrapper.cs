using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamAccessWrapper<T> : AbstractAccessWrapper
        where T : unmanaged, IEntityProxyInstance
    {
        public DataStream<T> DataStream { get; }
        public byte Context { get; }

        public DataStreamAccessWrapper(DataStream<T> dataStream, 
                                       AccessType accessType, 
                                       AbstractJobConfig.Usage usage, 
                                       byte context = byte.MaxValue) : base(accessType, usage)
        {
            DataStream = dataStream;
            Context = context;
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
