using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamPendingAccessWrapper<T> : AbstractDataStreamAccessWrapper<T>
        where T : unmanaged, IEntityProxyInstance
    {
        public DataStreamPendingAccessWrapper(EntityProxyDataStream<T> dataStream, 
                                              AccessType accessType, 
                                              AbstractJobConfig.Usage usage) 
            : base(dataStream, accessType, usage)
        {
        }

        public override JobHandle Acquire()
        {
            return DataStream.AcquirePendingAsync(AccessType);
        }

        public override void Release(JobHandle dependsOn)
        {
            DataStream.ReleasePendingAsync(dependsOn);
        }
    }
}
