using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IEntityPersistentDataInstance
    {
        public void DisposeForEntity(Entity entity);
    }
}
