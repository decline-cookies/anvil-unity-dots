namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ICancellableDataStream<TInstance> : IDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
