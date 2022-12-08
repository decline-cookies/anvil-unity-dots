using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteDataStream : AbstractArrayDataStream<EntityProxyInstanceID>
    {
        public CancelCompleteDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
        }
    }
}
