namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ICommonCancellableDataStream<TInstance> : ICommonDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
