namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ICommonDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
