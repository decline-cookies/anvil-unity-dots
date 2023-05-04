using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IDataOwner
    {
        public DataOwnerID WorldUniqueID { get; }
        public World World { get; }
    }
}
