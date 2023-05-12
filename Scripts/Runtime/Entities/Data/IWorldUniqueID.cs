using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Provides a <see cref="World"/> unique ID
    /// </summary>
    /// <typeparam name="TWorldUniqueID">The type of ID</typeparam>
    public interface IWorldUniqueID<out TWorldUniqueID>
    {
        /// <summary>
        /// The <see cref="World"/> unique ID
        /// </summary>
        public TWorldUniqueID WorldUniqueID { get; }
    }
}