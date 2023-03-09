namespace Anvil.Unity.DOTS.Entities
{
    public interface IWorldEntityPersistentData<in T> : IEntityPersistentData<T>
        where T : struct, IEntityPersistentDataInstance
    {
        
    }
}
