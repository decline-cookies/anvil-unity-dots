namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
