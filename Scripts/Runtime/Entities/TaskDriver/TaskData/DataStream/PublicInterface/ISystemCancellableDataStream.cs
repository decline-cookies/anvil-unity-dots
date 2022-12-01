namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ISystemCancellableDataStream<TInstance> : ISystemDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
