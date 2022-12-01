namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IDriverCancellableDataStream<TInstance> : IDriverDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
