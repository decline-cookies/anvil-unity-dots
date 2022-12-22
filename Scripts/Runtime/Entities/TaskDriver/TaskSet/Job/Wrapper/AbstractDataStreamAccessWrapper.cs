using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStreamAccessWrapper<T> : AbstractAccessWrapper
        where T : unmanaged, IEntityProxyInstance
    {
        public EntityProxyDataStream<T> DataStream { get; }

        protected AbstractDataStreamAccessWrapper(EntityProxyDataStream<T> dataStream,
                                                  AccessType accessType,
                                                  AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            DataStream = dataStream;
        }
    }
}
