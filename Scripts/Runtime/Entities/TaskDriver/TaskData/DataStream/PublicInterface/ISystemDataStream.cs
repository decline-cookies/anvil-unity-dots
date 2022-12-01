namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ISystemDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
