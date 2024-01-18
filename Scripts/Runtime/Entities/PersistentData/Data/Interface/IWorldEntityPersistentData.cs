using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// An <see cref="IEntityPersistentData{T}"/> that is owned by a <see cref="World"/>.
    /// There is only ever one of these per <see cref="World"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IEntityPersistentDataInstance"/></typeparam>
    public interface IWorldEntityPersistentData<T> : IEntityPersistentData<T>
        where T : unmanaged, IEntityPersistentDataInstance
    {
        
    }
}
