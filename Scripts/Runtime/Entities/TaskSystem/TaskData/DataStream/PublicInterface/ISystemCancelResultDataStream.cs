namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ISystemCancelResultDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
