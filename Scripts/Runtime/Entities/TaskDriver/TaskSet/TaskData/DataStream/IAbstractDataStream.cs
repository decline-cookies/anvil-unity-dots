namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IAbstractDataStream<TInstance> : IAbstractDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
    
    
    /// <summary>
    /// Represents a stream of data
    /// </summary>
    public interface IAbstractDataStream
    {
        
    }
}
