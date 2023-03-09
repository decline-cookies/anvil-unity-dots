namespace Anvil.Unity.DOTS.Entities
{
    public interface IDriverEntityPersistentData<in T> : IEntityPersistentData<T>
        where T : struct, IEntityPersistentDataInstance
    {
    }
}
