using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Provides a <see cref="World"/> unique ID.
    /// This ID is unique within the current World instance.
    /// This ID is not unique globally between all world instances.
    /// </summary>
    /// <typeparam name="TWorldUniqueID">The type of ID</typeparam>
    public interface IWorldUniqueID<out TWorldUniqueID>
    {
        /// <summary>
        /// The <see cref="World"/> unique ID.
        /// This ID is unique within the current World instance.
        /// This ID is not unique globally between all world instances.
        /// </summary>
        public TWorldUniqueID WorldUniqueID { get; }
    }
}