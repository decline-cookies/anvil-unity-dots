using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractEntityInstanceIDDataStream : AbstractConsolidatableDataStream
    {
        internal UnsafeTypedStream<EntityProxyInstanceID> Pending { get; }

        protected AbstractEntityInstanceIDDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            Pending = new UnsafeTypedStream<EntityProxyInstanceID>(Allocator.Persistent);
        }

        protected override void DisposeDataStream()
        {
            Pending.Dispose();
            base.DisposeDataStream();
        }
    }
}
