using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    public readonly struct EntityPersistentDataReader<TData> 
        where TData : struct, IEntityPersistentDataInstance
    {
        [ReadOnly] private readonly UnsafeParallelHashMap<Entity, TData> m_Lookup;

        internal EntityPersistentDataReader(ref UnsafeParallelHashMap<Entity, TData> lookup)
        {
            m_Lookup = lookup;
        }

        public TData this[Entity entity]
        {
            get => m_Lookup[entity];
        }
    }
}