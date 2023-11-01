using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// An <see cref="IEntityPersistentData{T}"/> that is owned by a <see cref="AbstractTaskDriverSystem"/>.
    /// There is only ever one of these per type per <see cref="World"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IEntityPersistentDataInstance"/></typeparam>
    public interface ISystemEntityPersistentData<T> : IEntityPersistentData<T>
        where T : unmanaged, IEntityPersistentDataInstance
    {
        
    }
}
