using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class DataStreamPendingCancelActiveAccessWrapper<T> : AbstractDataStreamAccessWrapper<T>,
                                                                   IDataStreamPendingAccessWrapper
        where T : unmanaged, IEntityProxyInstance
    {
        public DataStreamPendingCancelActiveAccessWrapper(EntityProxyDataStream<T> dataStream, AccessType accessType, AbstractJobConfig.Usage usage) : base(dataStream, accessType, usage)
        {
        }

        public override JobHandle AcquireAsync()
        {
            return DataStream.AcquirePendingCancelActiveAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            DataStream.ReleasePendingCancelActiveAsync(dependsOn);
        }
    }
}
