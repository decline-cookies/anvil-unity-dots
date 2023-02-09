using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// An <see cref="IAbstractDataStream{TInstance}"/> that is owned by a <see cref="AbstractTaskDriverSystem"/>.
    /// There is only ever one of these per type per <see cref="World"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/></typeparam>
    public interface ISystemDataStream<TInstance> : IAbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance { }
}
