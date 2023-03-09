using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityPersistentData<T> : AbstractTypedPersistentData<UnsafeParallelHashMap<Entity, T>>,
                                             IDriverEntityPersistentData<T>,
                                             ISystemEntityPersistentData<T>,
                                             IWorldEntityPersistentData<T>
        where T : struct, IEntityPersistentDataInstance
    {
        public EntityPersistentData()
            : base(new UnsafeParallelHashMap<Entity, T>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent))
        {
        }

        protected override void DisposeData()
        {
            ref UnsafeParallelHashMap<Entity, T> data = ref Data;
            foreach (KeyValue<Entity, T> entry in data)
            {
                entry.Value.DisposeForEntity(entry.Key);
            }
            base.DisposeData();
        }

        public void Add(Entity entity, T data)
        {
            Data.Add(entity, data);
        }

        public EntityPersistentDataReader<T> CreateEntityPersistentDataReader()
        {
            return new EntityPersistentDataReader<T>(ref Data);
        }

        public EntityPersistentDataWriter<T> CreateEntityPersistentDataWriter()
        {
            return new EntityPersistentDataWriter<T>(ref Data);
        }
    }
}
