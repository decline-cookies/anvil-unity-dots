using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractDataStreamActiveAccessWrapper<T> : AbstractAccessWrapper
        where T : unmanaged, IEntityKeyedTask
    {
        public EntityProxyDataStream<T> DataStream { get; }

        protected AbstractDataStreamActiveAccessWrapper(EntityProxyDataStream<T> dataStream, AccessType accessType, AbstractJobConfig.Usage usage)
            : base(accessType, usage)
        {
            DataStream = dataStream;
        }
    }
}