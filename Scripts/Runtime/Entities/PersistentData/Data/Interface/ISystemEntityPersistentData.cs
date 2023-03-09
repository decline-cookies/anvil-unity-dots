namespace Anvil.Unity.DOTS.Entities
{
    public interface ISystemEntityPersistentData<T> : IEntityPersistentData<T>
        where T : struct, IEntityPersistentDataInstance
    {
        
    }
}
