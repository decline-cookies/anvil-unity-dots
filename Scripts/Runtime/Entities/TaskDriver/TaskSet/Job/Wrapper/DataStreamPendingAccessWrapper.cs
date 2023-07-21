using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class DataStreamPendingAccessWrapper<T> : AbstractDataStreamPendingAccessWrapper<EntityProxyDataStream<T>>, IDataStreamPendingAccessWrapper
        where T : unmanaged, IEntityKeyedTask
    {
        public DataStreamPendingAccessWrapper(EntityProxyDataStream<T> dataStream, AccessType accessType, AbstractJobConfig.Usage usage)
            : base(dataStream, accessType, usage) { }

        public override JobHandle AcquireAsync()
        {
            return m_DefaultStream.AcquirePendingAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            m_DefaultStream.ReleasePendingAsync(dependsOn);
        }
    }
}