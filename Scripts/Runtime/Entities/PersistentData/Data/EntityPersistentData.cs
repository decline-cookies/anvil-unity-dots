using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityPersistentData<T> : AbstractTypedPersistentData<UnsafeParallelHashMap<Entity, T>>,
                                             IEntityPersistentData<T>
        where T : struct
    {
        private readonly IEntityPersistentData<T>.DisposalCallbackPerEntity m_DisposalCallbackPerEntity;

        public EntityPersistentData(
            uint id,
            IEntityPersistentData<T>.DisposalCallbackPerEntity disposalCallbackPerEntity)
            : base(
                id,
                new UnsafeParallelHashMap<Entity, T>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent))
        {
            m_DisposalCallbackPerEntity = disposalCallbackPerEntity;
        }

        protected override void DisposeData()
        {
            if (m_DisposalCallbackPerEntity != null)
            {
                ref UnsafeParallelHashMap<Entity, T> data = ref Data;
                foreach (KeyValue<Entity, T> entry in data)
                {
                    m_DisposalCallbackPerEntity(entry.Key, entry.Value);
                }
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
