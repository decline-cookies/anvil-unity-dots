using Anvil.Unity.DOTS.Entities.TaskDriver;
using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// An <see cref="IAbstractPersistentData"/> typed to a specific <see cref="IEntityPersistentDataInstance"/>
    /// that exposes read-only access.
    /// The data is associated with an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IEntityPersistentDataInstance"/></typeparam>
    public interface IReadOnlyEntityPersistentData<T> : IAbstractPersistentData,
                                                        IReadAccessControlledValue<EntityPersistentDataReader<T>>
        where T : unmanaged, IEntityPersistentDataInstance { }
}