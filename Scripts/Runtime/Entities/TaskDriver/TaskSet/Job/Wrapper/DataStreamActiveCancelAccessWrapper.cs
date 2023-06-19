using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class DataStreamActiveCancelAccessWrapper<T> : AbstractDataStreamAccessWrapper<T>,
                                                            IDataStreamPendingAccessWrapper
        where T : unmanaged, IEntityProxyInstance
    {
        public DataStreamActiveCancelAccessWrapper(EntityProxyDataStream<T> dataStream, AccessType accessType, AbstractJobConfig.Usage usage) : base(dataStream, accessType, usage) { }

        public override JobHandle AcquireAsync()
        {
            return DataStream.AcquireActiveCancelAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            DataStream.ReleaseActiveCancelAsync(dependsOn);
        }
    }
}