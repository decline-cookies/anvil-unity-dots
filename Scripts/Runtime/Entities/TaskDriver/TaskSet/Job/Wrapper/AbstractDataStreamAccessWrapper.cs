using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractDataStreamAccessWrapper<T> : AbstractAccessWrapper
        where T : unmanaged, IEntityKeyedTask
    {
        public EntityProxyDataStream<T> DataStream { get; }

        protected AbstractDataStreamAccessWrapper(EntityProxyDataStream<T> dataStream, AccessType accessType, AbstractJobConfig.Usage usage)
            : base(accessType, usage)
        {
            DataStream = dataStream;
        }
    }
}