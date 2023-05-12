using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IDataOwner : IWorldUniqueID<DataOwnerID>
    {
        public World World { get; }
    }
}
