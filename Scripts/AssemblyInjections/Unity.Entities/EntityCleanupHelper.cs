using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public static class EntityCleanupHelper
    {
        public static readonly ComponentType CLEAN_UP_ENTITY_COMPONENT_TYPE = ComponentType.ReadOnly<CleanupEntity>();
    }
}
