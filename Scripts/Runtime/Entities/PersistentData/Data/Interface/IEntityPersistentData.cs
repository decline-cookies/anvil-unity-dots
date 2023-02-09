using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IEntityPersistentData<in T> : IAbstractPersistentData
        where T : struct
    {
        public delegate void DisposalCallbackPerEntity(Entity entity, T data);

        public void Add(Entity entity, T data);
    }
}
