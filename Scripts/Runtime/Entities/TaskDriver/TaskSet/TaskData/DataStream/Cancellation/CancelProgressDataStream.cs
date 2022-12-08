using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelProgressDataStream : AbstractLookupDataStream<EntityProxyInstanceID>
    {
        public CancelProgressDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
        }
    }
}
