namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// An <see cref="IAbstractDataStream"/> typed to specific <see cref="IEntityProxyInstance"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/></typeparam>
    public interface IAbstractDataStream<TInstance> : IAbstractDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
    
    
    /// <summary>
    /// Represents a stream of data with two parts.
    /// The first is a parallel writable collection to be able to write pending instances to.
    /// The second is a narrow array collection for reading.
    /// </summary>
    public interface IAbstractDataStream
    {
        
    }
}
