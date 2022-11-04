using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractEntityInstanceIDDataStream : AbstractConsolidatableDataStream
    {
        //Deliberately NOT getters because that messes up what the Safety Handle points to. 
        //TODO: Elaborate
        internal UnsafeTypedStream<EntityProxyInstanceID> Pending;

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
