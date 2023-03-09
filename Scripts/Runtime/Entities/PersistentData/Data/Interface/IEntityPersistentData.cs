using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IEntityPersistentData<in T> : IAbstractPersistentData
        where T : struct, IEntityPersistentDataInstance
    {
        public void Add(Entity entity, T data);
    }
}
