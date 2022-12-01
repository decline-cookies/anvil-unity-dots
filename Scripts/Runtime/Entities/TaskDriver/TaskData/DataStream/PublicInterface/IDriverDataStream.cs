namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IDriverDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
