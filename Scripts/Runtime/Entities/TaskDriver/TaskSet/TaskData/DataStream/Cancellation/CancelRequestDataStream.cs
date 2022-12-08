using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestDataStream : AbstractLookupDataStream<EntityProxyInstanceID>
    {
        public CancelRequestDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
        }
    }
}
