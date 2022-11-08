namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ICancelResultDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
