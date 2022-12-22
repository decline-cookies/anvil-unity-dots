using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamActiveAccessWrapper<T> : AbstractDataStreamAccessWrapper<T>
        where T : unmanaged, IEntityProxyInstance
    {
        public DataStreamActiveAccessWrapper(EntityProxyDataStream<T> dataStream, 
                                             AccessType accessType,
                                             AbstractJobConfig.Usage usage) : 
            base(dataStream, accessType, usage)
        {
        }

        public override JobHandle Acquire()
        {
            return DataStream.AcquireActiveAsync(AccessType);
        }

        public override void Release(JobHandle dependsOn)
        {
            DataStream.ReleaseActiveAsync(dependsOn);
        }
    }
}
