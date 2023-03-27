using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    internal readonly struct EntityLifecycleFilteredGroup
    {
        public readonly EntityQueryMask Mask;
        public readonly UnsafeList<Entity> FilteredEntities;

        public EntityLifecycleFilteredGroup(EntityQueryMask mask, UnsafeList<Entity> filteredEntities)
        {
            Mask = mask;
            FilteredEntities = filteredEntities;
        }
    }
}
