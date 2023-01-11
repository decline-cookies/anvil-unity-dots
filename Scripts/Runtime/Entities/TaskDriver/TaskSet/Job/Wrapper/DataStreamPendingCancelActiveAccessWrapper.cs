using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamPendingCancelActiveAccessWrapper<T> : AbstractDataStreamAccessWrapper<T>
        where T : unmanaged, IEntityProxyInstance
    {
        public DataStreamPendingCancelActiveAccessWrapper(EntityProxyDataStream<T> dataStream, AccessType accessType, AbstractJobConfig.Usage usage) : base(dataStream, accessType, usage)
        {
        }

        public override JobHandle Acquire()
        {
            return DataStream.AcquirePendingCancelActiveAsync(AccessType);
        }

        public override void Release(JobHandle dependsOn)
        {
            DataStream.ReleasePendingCancelActiveAsync(dependsOn);
        }
    }
}
