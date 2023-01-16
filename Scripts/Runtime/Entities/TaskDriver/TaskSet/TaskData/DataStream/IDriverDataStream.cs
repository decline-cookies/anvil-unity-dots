using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// An <see cref="IAbstractDataStream{TInstance}"/> that is owned by a <see cref="AbstractTaskDriver"/>.
    /// There are as many of these per type per <see cref="World"/> as there are corresponding
    /// <see cref="AbstractTaskDriver"/>s.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/></typeparam>
    public interface IDriverDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        
    }
}
