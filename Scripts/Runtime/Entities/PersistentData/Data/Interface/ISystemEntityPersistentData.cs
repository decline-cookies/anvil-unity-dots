namespace Anvil.Unity.DOTS.Entities
{
    public interface ISystemEntityPersistentData<in T> : IEntityPersistentData<T>
        where T : struct, IEntityPersistentDataInstance
    {
        
    }
}
