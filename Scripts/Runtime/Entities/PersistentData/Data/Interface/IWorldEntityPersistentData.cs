namespace Anvil.Unity.DOTS.Entities
{
    public interface IWorldEntityPersistentData<T> : IEntityPersistentData<T>
        where T : struct, IEntityPersistentDataInstance
    {
        
    }
}
