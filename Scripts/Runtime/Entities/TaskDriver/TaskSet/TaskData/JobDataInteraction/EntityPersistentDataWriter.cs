using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public struct EntityPersistentDataWriter<TData>
        where TData : struct
    {
        [NativeDisableContainerSafetyRestriction][NativeDisableParallelForRestriction] private UnsafeParallelHashMap<Entity, TData> m_Lookup;

        internal EntityPersistentDataWriter(ref UnsafeParallelHashMap<Entity, TData> lookup)
        {
            m_Lookup = lookup;
        }

        public TData this[Entity entity]
        {
            get => m_Lookup[entity];
            set => m_Lookup[entity] = value;
        }
    }
}
