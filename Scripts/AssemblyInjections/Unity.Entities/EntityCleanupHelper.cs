using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class to detect when an Entity has entered a clean up state due to having
    /// <see cref="ISystemStateComponentData"/> attached.
    /// </summary>
    public static class EntityCleanupHelper
    {
        /// <summary>
        /// Provides the type of <see cref="CleanupEntity"/> so it can be used in queries outside the Unity assembly.
        /// </summary>
        public static readonly ComponentType CLEAN_UP_ENTITY_COMPONENT_TYPE = ComponentType.ReadOnly<CleanupEntity>();
    }
}
