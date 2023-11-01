using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// An <see cref="IEntityPersistentData{T}"/> that is owned by a <see cref="AbstractTaskDriver"/>
    /// There are as many of these per type per <see cref="World"/> as there are corresponding
    /// <see cref="AbstractTaskDriver"/>s.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IEntityPersistentDataInstance"/></typeparam>
    public interface IDriverEntityPersistentData<T> : IEntityPersistentData<T>
        where T : unmanaged, IEntityPersistentDataInstance
    {
    }
}
