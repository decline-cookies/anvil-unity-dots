using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents an instance of data that is keyed off an associated <see cref="Entity"/>
    /// for use in <see cref="IEntityPersistentData{T}"/> type.
    /// </summary>
    public interface IEntityPersistentDataInstance
    {
        /// <summary>
        /// Called whenever an element is being disposed during cleanup.
        /// This allows for custom cleanup code.
        /// </summary>
        /// <param name="entity">The entity that was associated with this data</param>
        public void DisposeForEntity(Entity entity);
    }
}
