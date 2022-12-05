namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ICommonCancelResultDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
