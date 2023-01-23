using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
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

        public override JobHandle AcquireAsync()
        {
            return DataStream.AcquirePendingAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            DataStream.ReleasePendingAsync(dependsOn);
        }
    }
}
