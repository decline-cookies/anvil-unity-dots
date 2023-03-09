namespace Anvil.Unity.DOTS.Entities
{
    public interface IDriverEntityPersistentData<T> : IEntityPersistentData<T>
        where T : struct, IEntityPersistentDataInstance
    {
    }
}
